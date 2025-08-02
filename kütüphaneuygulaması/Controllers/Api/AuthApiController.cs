using Microsoft.AspNetCore.Mvc;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Models.DTOs;
using kütüphaneuygulaması.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Controllers.Api
{
    /// <summary>
    /// JWT Authentication için API endpoint'leri
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IValidationService _validationService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            IJwtService jwtService,
            kütüphaneuygulamasıContext context,
            IPasswordService passwordService,
            IValidationService validationService,
            ILogger<AuthApiController> logger)
        {
            _jwtService = jwtService;
            _context = context;
            _passwordService = passwordService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcı girişi ve JWT token oluşturma
        /// </summary>
        /// <param name="loginRequest">Giriş bilgileri</param>
        /// <returns>JWT token bilgileri</returns>
        /// <response code="200">Başarılı giriş</response>
        /// <response code="400">Geçersiz giriş bilgileri</response>
        /// <response code="401">Kimlik doğrulama başarısız</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponseDto<JwtTokenDto>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        public async Task<ActionResult<ApiResponseDto<JwtTokenDto>>> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz giriş bilgileri",
                        Errors = errors
                    });
                }

                // Input sanitization
                loginRequest.Username = _validationService.SanitizeInput(loginRequest.Username);
                loginRequest.Password = _validationService.SanitizeInput(loginRequest.Password);

                // JWT token oluştur
                var tokenDto = await _jwtService.GenerateTokenAsync(loginRequest.Username, loginRequest.Password);

                _logger.LogInformation("Başarılı JWT girişi: {Username}", loginRequest.Username);

                return Ok(new ApiResponseDto<JwtTokenDto>
                {
                    Success = true,
                    Message = "Giriş başarılı",
                    Data = tokenDto
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("JWT giriş hatası: {Message}", ex.Message);
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT giriş işlemi sırasında hata");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Giriş işlemi sırasında bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Kullanıcı kaydı
        /// </summary>
        /// <param name="registerRequest">Kayıt bilgileri</param>
        /// <returns>Kayıt sonucu</returns>
        /// <response code="201">Başarılı kayıt</response>
        /// <response code="400">Geçersiz kayıt bilgileri</response>
        /// <response code="409">Kullanıcı adı veya email zaten mevcut</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponseDto<UserInfoDto>), 201)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 409)]
        public async Task<ActionResult<ApiResponseDto<UserInfoDto>>> Register([FromBody] RegisterRequestDto registerRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz kayıt bilgileri",
                        Errors = errors
                    });
                }

                // Input sanitization
                registerRequest.Username = _validationService.SanitizeInput(registerRequest.Username);
                registerRequest.Email = _validationService.SanitizeInput(registerRequest.Email);

                // Kullanıcı adı kontrolü
                var existingUser = await _context.usersaccounts
                    .FirstOrDefaultAsync(u => u.name == registerRequest.Username || u.email == registerRequest.Email);

                if (existingUser != null)
                {
                    var conflictMessage = existingUser.name == registerRequest.Username 
                        ? "Bu kullanıcı adı zaten kullanılıyor" 
                        : "Bu email adresi zaten kullanılıyor";

                    return Conflict(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = conflictMessage
                    });
                }

                // Yeni kullanıcı oluştur
                var newUser = new usersaccounts
                {
                    name = registerRequest.Username,
                    email = registerRequest.Email,
                    pass = _passwordService.HashPassword(registerRequest.Password),
                    role = registerRequest.Role,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.usersaccounts.Add(newUser);
                await _context.SaveChangesAsync();

                var userInfo = new UserInfoDto
                {
                    Id = newUser.Id,
                    Username = newUser.name,
                    Email = newUser.email,
                    Role = newUser.role,
                    CreatedDate = newUser.CreatedDate
                };

                _logger.LogInformation("Yeni kullanıcı kaydı: {Username}", registerRequest.Username);

                return CreatedAtAction(nameof(GetUserInfo), new { id = newUser.Id }, new ApiResponseDto<UserInfoDto>
                {
                    Success = true,
                    Message = "Kullanıcı başarıyla oluşturuldu",
                    Data = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında hata");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Kayıt işlemi sırasında bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Refresh token ile yeni access token alma
        /// </summary>
        /// <param name="refreshRequest">Refresh token</param>
        /// <returns>Yeni JWT token bilgileri</returns>
        /// <response code="200">Token yenilendi</response>
        /// <response code="401">Geçersiz refresh token</response>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponseDto<JwtTokenDto>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        public async Task<ActionResult<ApiResponseDto<JwtTokenDto>>> RefreshToken([FromBody] RefreshTokenRequestDto refreshRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz refresh token"
                    });
                }

                var tokenDto = await _jwtService.RefreshTokenAsync(refreshRequest.RefreshToken);

                return Ok(new ApiResponseDto<JwtTokenDto>
                {
                    Success = true,
                    Message = "Token başarıyla yenilendi",
                    Data = tokenDto
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Refresh token hatası: {Message}", ex.Message);
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme sırasında hata");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Token yenileme sırasında bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Kullanıcı bilgilerini getir
        /// </summary>
        /// <param name="id">Kullanıcı ID</param>
        /// <returns>Kullanıcı bilgileri</returns>
        /// <response code="200">Kullanıcı bilgileri</response>
        /// <response code="404">Kullanıcı bulunamadı</response>
        [HttpGet("user/{id}")]
        [ProducesResponseType(typeof(ApiResponseDto<UserInfoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
        public async Task<ActionResult<ApiResponseDto<UserInfoDto>>> GetUserInfo(int id)
        {
            try
            {
                var user = await _context.usersaccounts
                    .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

                if (user == null)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Kullanıcı bulunamadı"
                    });
                }

                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.name,
                    Email = user.email,
                    Role = user.role,
                    CreatedDate = user.CreatedDate
                };

                return Ok(new ApiResponseDto<UserInfoDto>
                {
                    Success = true,
                    Message = "Kullanıcı bilgileri başarıyla getirildi",
                    Data = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgileri getirilirken hata: {UserId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Kullanıcı bilgileri getirilirken bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Token'ı iptal et (logout)
        /// </summary>
        /// <param name="refreshRequest">İptal edilecek refresh token</param>
        /// <returns>İptal sonucu</returns>
        /// <response code="200">Token başarıyla iptal edildi</response>
        /// <response code="400">Geçersiz token</response>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        public async Task<ActionResult<ApiResponseDto<object>>> Logout([FromBody] RefreshTokenRequestDto refreshRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz token"
                    });
                }

                var result = await _jwtService.RevokeTokenAsync(refreshRequest.RefreshToken);

                if (!result)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz veya zaten iptal edilmiş token"
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Başarıyla çıkış yapıldı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çıkış işlemi sırasında hata");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Çıkış işlemi sırasında bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Token doğrulama
        /// </summary>
        /// <param name="token">Doğrulanacak token</param>
        /// <returns>Doğrulama sonucu</returns>
        /// <response code="200">Token geçerli</response>
        /// <response code="401">Token geçersiz</response>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        public async Task<ActionResult<ApiResponseDto<object>>> ValidateToken([FromBody] RefreshTokenRequestDto token)
        {
            try
            {
                var isValid = await _jwtService.ValidateTokenAsync(token.RefreshToken);

                if (!isValid)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Geçersiz token"
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Token geçerli"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token doğrulama sırasında hata");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Token doğrulama sırasında bir hata oluştu"
                });
            }
        }
    }
} 