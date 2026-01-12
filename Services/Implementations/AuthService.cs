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

namespace Hesapix.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            // Email kontrolü
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new Exception("Bu e-posta adresi zaten kayıtlı");
            }

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
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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
                throw new Exception("E-posta veya şifre hatalı");
            }

            // Şifre kontrolü
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new Exception("E-posta veya şifre hatalı");
            }

            // Hesap aktif mi kontrol et
            if (!user.IsActive)
            {
                throw new Exception("Hesabınız deaktif durumda. Lütfen destek ile iletişime geçin.");
            }

            // Abonelik kontrolü
            var hasActiveSubscription = user.Subscription != null &&
                                        user.Subscription.IsActive &&
                                        user.Subscription.EndDate > DateTime.UtcNow;

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
            // Email doğrulama mantığı
            // Bu kısım mail servisi entegrasyonu ile geliştirilecek
            return await Task.FromResult(true);
        }

        public async Task<bool> ResetPassword(string email, string newPassword, string resetToken)
        {
            // Şifre sıfırlama mantığı
            // Bu kısım mail servisi ve token doğrulama ile geliştirilecek
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        #region Private Methods

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))
            );
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
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
                TaxNumber = user.TaxNumber
            };
        }

        #endregion
    }
}