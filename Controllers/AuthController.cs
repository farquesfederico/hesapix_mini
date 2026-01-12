using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.Register(request);
                _logger.LogInformation("Yeni kullanıcı kaydı yapıldı: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt hatası: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı girişi
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.Login(request);
                _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Giriş başarısız: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Abonelik durumu kontrolü
        /// </summary>
        [Authorize]
        [HttpGet("check-subscription")]
        public async Task<IActionResult> CheckSubscription()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var hasSubscription = await _authService.CheckSubscription(userId);

                return Ok(new { hasActiveSubscription = hasSubscription });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik kontrolü hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı bilgileri
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var name = User.FindFirst(ClaimTypes.Name)?.Value;

                return Ok(new
                {
                    id = userId,
                    email = email,
                    fullName = name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgisi alma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Email doğrulama
        /// </summary>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var result = await _authService.VerifyEmail(request.Email, request.VerificationCode);

                if (result)
                {
                    return Ok(new { message = "Email başarıyla doğrulandı" });
                }

                return BadRequest(new { message = "Doğrulama başarısız" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Şifre sıfırlama
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPassword(request.Email, request.NewPassword, request.ResetToken);

                if (result)
                {
                    return Ok(new { message = "Şifre başarıyla sıfırlandı" });
                }

                return BadRequest(new { message = "Şifre sıfırlama başarısız" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama hatası");
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // Request models
    public class VerifyEmailRequest
    {
        public string Email { get; set; }
        public string VerificationCode { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ResetToken { get; set; }
    }
}