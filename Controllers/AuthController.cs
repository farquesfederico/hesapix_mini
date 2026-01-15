using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result.Data!, result.Message));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result.Data!, result.Message));
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await _authService.VerifyEmailAsync(token);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] string email)
    {
        var result = await _authService.ResendVerificationEmailAsync(email);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] string email)
    {
        var result = await _authService.ForgotPasswordAsync(email);
        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromQuery] string token, [FromBody] string newPassword)
    {
        var result = await _authService.ResetPasswordAsync(token, newPassword);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}