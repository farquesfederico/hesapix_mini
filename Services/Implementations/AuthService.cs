using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Hesapix.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IMapper mapper,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return (false, "Bu email adresi zaten kullanılıyor", null);
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CompanyName = request.CompanyName,
            TaxNumber = request.TaxNumber,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Role = UserRole.User,
            IsEmailVerified = false,
            EmailVerificationToken = GenerateRandomToken(),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={user.EmailVerificationToken}";
        await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

        return (true, "Kayıt başarılı. Lütfen email adresinizi doğrulayın.", null);
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return (false, "Email veya şifre hatalı", null);
        }

        if (!user.IsEmailVerified)
        {
            return (false, "Lütfen önce email adresinizi doğrulayın", null);
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.RefreshToken = GenerateRandomToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(
            int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!)
        );

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        var response = new AuthResponse
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:ExpirationMinutes"]!)
            ),
            User = userDto
        };

        return (true, "Giriş başarılı", response);
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return (false, "Geçersiz veya süresi dolmuş refresh token", null);
        }

        user.RefreshToken = GenerateRandomToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(
            int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!)
        );

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        var response = new AuthResponse
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:ExpirationMinutes"]!)
            ),
            User = userDto
        };

        return (true, "Token yenilendi", response);
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            return (false, "Geçersiz doğrulama linki");
        }

        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            return (false, "Doğrulama linkinin süresi dolmuş. Lütfen yeni bir link talep edin");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        await _context.SaveChangesAsync();

        return (true, "Email adresiniz başarıyla doğrulandı. Şimdi giriş yapabilirsiniz");
    }

    public async Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return (false, "Kullanıcı bulunamadı");
        }

        if (user.IsEmailVerified)
        {
            return (false, "Email adresi zaten doğrulanmış");
        }

        user.EmailVerificationToken = GenerateRandomToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        await _context.SaveChangesAsync();

        var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={user.EmailVerificationToken}";
        await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

        return (true, "Doğrulama emaili gönderildi");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return (true, "Eğer email adresi sistemimizde kayıtlıysa, şifre sıfırlama linki gönderildi");
        }

        user.PasswordResetToken = GenerateRandomToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        var resetLink = $"{_configuration["AppSettings:FrontendUrl"]}/reset-password?token={user.PasswordResetToken}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        return (true, "Eğer email adresi sistemimizde kayıtlıysa, şifre sıfırlama linki gönderildi");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

        if (user == null)
        {
            return (false, "Geçersiz sıfırlama linki");
        }

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return (false, "Sıfırlama linkinin süresi dolmuş. Lütfen yeni bir link talep edin");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return (true, "Şifreniz başarıyla güncellendi. Şimdi giriş yapabilirsiniz");
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("CompanyName", user.CompanyName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}