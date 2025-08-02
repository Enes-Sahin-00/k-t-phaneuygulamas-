using System.Security.Claims;
using kütüphaneuygulaması.Models.DTOs;

namespace kütüphaneuygulaması.Services
{
    public interface IJwtService
    {
        Task<JwtTokenDto> GenerateTokenAsync(string username, string password);
        Task<JwtTokenDto> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserInfoDto?> GetUserFromTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> RevokeAllUserTokensAsync(int userId);
        string GenerateAccessToken(UserInfoDto user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateAccessToken(string token);
    }
} 