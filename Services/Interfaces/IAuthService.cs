using Hesapix.Models.DTOs.Auth;

namespace Hesapix.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> Register(RegisterRequest request);
        Task<AuthResponse> Login(LoginRequest request);
        Task<bool> CheckSubscription(int userId);
        Task<bool> VerifyEmail(string email, string verificationCode);
        Task<bool> RequestPasswordReset(string email);
        Task<bool> ResetPassword(string email, string newPassword, string resetToken);
    }
}