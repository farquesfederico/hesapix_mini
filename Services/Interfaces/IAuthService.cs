using Hesapix.Models.Common;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Auth;

namespace Hesapix.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(int userId);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<ApiResponse<bool>> UpdateUserRoleAsync(int userId, string role);
        Task<ApiResponse<List<UserDto>>> GetAllUsersAsync(int page, int pageSize, string? search);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId);
    }
}