using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AuthResponse>.FailResult("Geçersiz veri"));
                }

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Yeni kullanıcı kaydedildi: {Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sırasında hata");
                return StatusCode(500, ApiResponse<AuthResponse>.FailResult("Kayıt işlemi başarısız"));
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AuthResponse>.FailResult("Geçersiz veri"));
                }

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    return Unauthorized(result);
                }

                _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş sırasında hata");
                return StatusCode(500, ApiResponse<AuthResponse>.FailResult("Giriş işlemi başarısız"));
            }
        }

        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<AuthResponse>.FailResult("Geçersiz token"));
                }

                var result = await _authService.RefreshTokenAsync(userId);

                if (!result.Success)
                {
                    return Unauthorized(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme sırasında hata");
                return StatusCode(500, ApiResponse<AuthResponse>.FailResult("Token yenileme başarısız"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<bool>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Kullanıcı şifresini değiştirdi: UserId={UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirme sırasında hata");
                return StatusCode(500, ApiResponse<bool>.FailResult("Şifre değiştirme başarısız"));
            }
        }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}