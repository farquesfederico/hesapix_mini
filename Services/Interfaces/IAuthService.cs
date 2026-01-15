using Hesapix.Models.DTOs.Auth;

namespace Hesapix.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(string refreshToken);
    Task<(bool Success, string Message)> VerifyEmailAsync(string token);
    Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email);
    Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
}