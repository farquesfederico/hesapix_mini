using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.Common;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Yeni kullanıcı kaydı
        /// </summary>
        /// <param name="request">Kayıt bilgileri</param>
        /// <returns>JWT token ve kullanıcı bilgileri</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.Register(request);
            _logger.LogInformation("Yeni kullanıcı kaydı yapıldı: {Email}", request.Email);

            return Ok(ApiResponse<AuthResponse>.SuccessResponse(
                response,
                "Kayıt başarılı. Email adresinizi doğrulamayı unutmayın."));
        }

        /// <summary>
        /// Kullanıcı girişi
        /// </summary>
        /// <param name="request">Giriş bilgileri</param>
        /// <returns>JWT token ve kullanıcı bilgileri</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.Login(request);
            _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", request.Email);

            return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Giriş başarılı"));
        }

        /// <summary>
        /// Abonelik durumu kontrolü
        /// </summary>
        /// <returns>Abonelik durumu</returns>
        [Authorize]
        [HttpGet("check-subscription")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckSubscription()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var hasSubscription = await _authService.CheckSubscription(userId);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { hasActiveSubscription = hasSubscription },
                "Abonelik durumu kontrol edildi"));
        }

        /// <summary>
        /// Mevcut kullanıcı bilgileri
        /// </summary>
        /// <returns>Kullanıcı bilgileri</returns>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailVerified = bool.Parse(User.FindFirst("EmailVerified")?.Value ?? "false");

            var userData = new
            {
                id = userId,
                email = email,
                fullName = name,
                emailVerified = emailVerified
            };

            return Ok(ApiResponse<object>.SuccessResponse(userData, "Kullanıcı bilgileri alındı"));
        }

        /// <summary>
        /// Email doğrulama
        /// </summary>
        /// <param name="request">Email ve doğrulama kodu</param>
        /// <returns>Doğrulama sonucu</returns>
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var result = await _authService.VerifyEmail(request.Email, request.VerificationCode);

            if (result)
            {
                _logger.LogInformation("Email doğrulandı: {Email}", request.Email);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Email başarıyla doğrulandı"));
            }

            return BadRequest(ApiResponse<object>.ErrorResponse("Doğrulama başarısız. Kod geçersiz veya süresi dolmuş."));
        }

        /// <summary>
        /// Şifre sıfırlama talebi
        /// </summary>
        /// <param name="request">Email adresi</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPost("request-password-reset")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
        {
            await _authService.RequestPasswordReset(request.Email);

            // Güvenlik: Her zaman başarılı dön
            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Eğer email adresi sistemde kayıtlıysa, şifre sıfırlama linki gönderildi."));
        }

        /// <summary>
        /// Şifre sıfırlama
        /// </summary>
        /// <param name="request">Şifre sıfırlama bilgileri</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPassword(
                request.Email,
                request.NewPassword,
                request.ResetToken);

            if (result)
            {
                _logger.LogInformation("Şifre sıfırlandı: {Email}", request.Email);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Şifre başarıyla sıfırlandı"));
            }

            return BadRequest(ApiResponse<object>.ErrorResponse("Şifre sıfırlama başarısız. Token geçersiz veya süresi dolmuş."));
        }

        /// <summary>
        /// Token yenileme (gelecekte eklenebilir)
        /// </summary>
        [Authorize]
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken()
        {
            // Refresh token implementasyonu
            await Task.CompletedTask;
            return Ok(ApiResponse<object>.SuccessResponse(null, "Token yenilendi"));
        }

        /// <summary>
        /// Çıkış (client-side token silme için)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            _logger.LogInformation("Kullanıcı çıkış yaptı: {Email}", email);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Başarıyla çıkış yapıldı"));
        }
    }

    // Request Models
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class RequestPasswordResetRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }
}