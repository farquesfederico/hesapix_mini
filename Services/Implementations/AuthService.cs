using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hesapix.Data;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using BCrypt.Net;

namespace Hesapix.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            // Email kontrolü
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı");
            }

            // Şifre güvenlik kontrolü
            if (!IsPasswordStrong(request.Password))
            {
                throw new InvalidOperationException(
                    "Şifre en az 8 karakter, 1 büyük harf, 1 küçük harf ve 1 rakam içermelidir");
            }

            // Email doğrulama kodu oluştur
            var verificationCode = GenerateVerificationCode();
            var verificationExpiry = DateTime.UtcNow.AddHours(24);

            // Yeni kullanıcı oluştur
            var user = new User
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                CompanyName = request.CompanyName,
                TaxNumber = request.TaxNumber,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = false,
                EmailVerificationCode = verificationCode,
                EmailVerificationExpiry = verificationExpiry
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Doğrulama emaili gönder
            await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, verificationCode);

            _logger.LogInformation("New user registered: {Email}", request.Email);

            // JWT token oluştur
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = MapToUserDto(user),
                HasActiveSubscription = false,
                SubscriptionEndDate = null
            };
        }

        public async Task<AuthResponse> Login(LoginRequest request)
        {
            // Kullanıcıyı bul
            var user = await _context.Users
                .Include(u => u.Subscription)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                await Task.Delay(Random.Shared.Next(100, 500)); // Timing attack önlemi
                throw new UnauthorizedAccessException("E-posta veya şifre hatalı");
            }

            // Şifre kontrolü
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);

                // Failed login counter artır
                user.FailedLoginAttempts++;
                user.LastFailedLoginDate = DateTime.UtcNow;

                // 5 başarısız denemeden sonra hesabı kilitle
                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsActive = false;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("User account locked due to failed attempts: {Email}", request.Email);
                }

                await _context.SaveChangesAsync();
                await Task.Delay(Random.Shared.Next(100, 500)); // Timing attack önlemi
                throw new UnauthorizedAccessException("E-posta veya şifre hatalı");
            }

            // Lockout kontrolü
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingMinutes = (user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                throw new UnauthorizedAccessException(
                    $"Hesabınız kilitli. Lütfen {Math.Ceiling(remainingMinutes)} dakika sonra tekrar deneyin.");
            }

            // Hesap aktif mi kontrol et
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Hesabınız deaktif durumda. Lütfen destek ile iletişime geçin.");
            }

            // Başarılı giriş - counter'ları sıfırla
            user.FailedLoginAttempts = 0;
            user.LastLoginDate = DateTime.UtcNow;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            // Abonelik kontrolü
            var hasActiveSubscription = user.Subscription != null &&
                                        user.Subscription.IsActive &&
                                        user.Subscription.EndDate > DateTime.UtcNow;

            _logger.LogInformation("Successful login for user: {Email}", request.Email);

            // JWT token oluştur
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = MapToUserDto(user),
                HasActiveSubscription = hasActiveSubscription,
                SubscriptionEndDate = user.Subscription?.EndDate
            };
        }

        public async Task<bool> CheckSubscription(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription == null)
            {
                return false;
            }

            return subscription.EndDate > DateTime.UtcNow;
        }

        public async Task<bool> VerifyEmail(string email, string verificationCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return false;
            }

            if (user.EmailVerified)
            {
                return true;
            }

            if (user.EmailVerificationCode != verificationCode)
            {
                _logger.LogWarning("Invalid verification code for user: {Email}", email);
                return false;
            }

            if (user.EmailVerificationExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired verification code for user: {Email}", email);
                return false;
            }

            user.EmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiry = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified for user: {Email}", email);
            return true;
        }

        public async Task<bool> RequestPasswordReset(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Güvenlik: Email bulunamasa bile başarılı dön
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                return true;
            }

            // Reset token oluştur
            var resetToken = GenerateSecureToken();
            var resetExpiry = DateTime.UtcNow.AddHours(1);

            user.PasswordResetToken = resetToken;
            user.PasswordResetExpiry = resetExpiry;
            await _context.SaveChangesAsync();

            // Reset emaili gönder
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken);

            _logger.LogInformation("Password reset requested for user: {Email}", email);
            return true;
        }

        public async Task<bool> ResetPassword(string email, string newPassword, string resetToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.PasswordResetToken != resetToken)
            {
                return false;
            }

            if (user.PasswordResetExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired password reset token for user: {Email}", email);
                return false;
            }

            if (!IsPasswordStrong(newPassword))
            {
                throw new InvalidOperationException(
                    "Şifre en az 8 karakter, 1 büyük harf, 1 küçük harf ve 1 rakam içermelidir");
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successful for user: {Email}", email);
            return true;
        }

        #region Private Methods

        private string HashPassword(string password)
        {
            // BCrypt kullanarak güvenli hash
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUpperCase && hasLowerCase && hasDigit;
        }

        private string GenerateVerificationCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Key not configured"))
            );
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("EmailVerified", user.EmailVerified.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                
            };

            var tokenExpiration = DateTime.UtcNow.AddDays(7); // 7 gün

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CompanyName = user.CompanyName,
                TaxNumber = user.TaxNumber,
                EmailVerified = user.EmailVerified
            };
        }

        #endregion
    }
}