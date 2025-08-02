using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Models.DTOs;

namespace kütüphaneuygulaması.Services
{
    public class JwtService : IJwtService
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(
            kütüphaneuygulamasıContext context,
            IPasswordService passwordService,
            IConfiguration configuration,
            ILogger<JwtService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<JwtTokenDto> GenerateTokenAsync(string username, string password)
        {
            try
            {
                // Kullanıcıyı doğrula
                var user = await _context.usersaccounts
                    .FirstOrDefaultAsync(u => u.name == username && u.IsActive);

                if (user == null || !_passwordService.VerifyPassword(password, user.pass))
                {
                    throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre");
                }

                // Kullanıcı bilgilerini DTO'ya dönüştür
                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.name,
                    Email = user.email,
                    Role = user.role,
                    CreatedDate = user.CreatedDate
                };

                // Token'ları oluştur
                var accessToken = GenerateAccessToken(userInfo);
                var refreshToken = GenerateRefreshToken();

                // Refresh token'ı veritabanına kaydet
                await SaveRefreshTokenAsync(user.Id, refreshToken);

                return new JwtTokenDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 saat
                    TokenType = "Bearer",
                    User = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token oluşturulurken hata: {Username}", username);
                throw;
            }
        }

        public async Task<JwtTokenDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Refresh token'ı doğrula
                var storedToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

                if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş refresh token");
                }

                // Kullanıcı bilgilerini DTO'ya dönüştür
                var userInfo = new UserInfoDto
                {
                    Id = storedToken.User.Id,
                    Username = storedToken.User.name,
                    Email = storedToken.User.email,
                    Role = storedToken.User.role,
                    CreatedDate = storedToken.User.CreatedDate
                };

                // Yeni token'ları oluştur
                var newAccessToken = GenerateAccessToken(userInfo);
                var newRefreshToken = GenerateRefreshToken();

                // Eski refresh token'ı devre dışı bırak
                storedToken.IsActive = false;
                storedToken.RevokedAt = DateTime.UtcNow;

                // Yeni refresh token'ı kaydet
                await SaveRefreshTokenAsync(userInfo.Id, newRefreshToken);

                await _context.SaveChangesAsync();

                return new JwtTokenDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer",
                    User = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token yenilenirken hata");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured"));

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token doğrulama hatası");
                return false;
            }
        }

        public async Task<UserInfoDto?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var principal = ValidateAccessToken(token);
                if (principal == null) return null;

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return null;

                var user = await _context.usersaccounts
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null) return null;

                return new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.name,
                    Email = user.email,
                    Role = user.role,
                    CreatedDate = user.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token'dan kullanıcı bilgisi alınırken hata");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

                if (storedToken == null) return false;

                storedToken.IsActive = false;
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token iptal edilirken hata");
                return false;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(int userId)
        {
            try
            {
                var userTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsActive)
                    .ToListAsync();

                foreach (var token in userTokens)
                {
                    token.IsActive = false;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı token'ları iptal edilirken hata: {UserId}", userId);
                return false;
            }
        }

        public string GenerateAccessToken(UserInfoDto user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured"));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("UserId", user.Id.ToString()),
                new("Username", user.Username),
                new("Role", user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured"));

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Access token doğrulama hatası");
                return null;
            }
        }

        private async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 gün
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
        }
    }

    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public usersaccounts User { get; set; } = null!;
    }
} 