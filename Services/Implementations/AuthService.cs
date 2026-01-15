using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Hesapix.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IMapper mapper, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
                    return ApiResponse<AuthResponse>.FailResult("Bu email adresi zaten kullanılıyor");

                if (!IsPasswordStrong(request.Password))
                    return ApiResponse<AuthResponse>.FailResult("Şifre en az 8 karakter, 1 büyük harf, 1 küçük harf ve 1 rakam içermelidir");

                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email.ToLower(),
                    PasswordHash = HashPassword(request.Password),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var userDto = _mapper.Map<UserDto>(user);
                _logger.LogInformation("Yeni kullanıcı kaydedildi: {Email}", request.Email);

                return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse { Token = token, User = userDto }, "Kayıt başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sırasında hata");
                return ApiResponse<AuthResponse>.FailResult("Kayıt işlemi başarısız");
            }
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
                if (user == null)
                {
                    _logger.LogWarning("Giriş denemesi: {Email}", request.Email);
                    await Task.Delay(Random.Shared.Next(100, 500));
                    return ApiResponse<AuthResponse>.FailResult("Email veya şifre hatalı");
                }

                if (!user.IsActive)
                    return ApiResponse<AuthResponse>.FailResult("Hesabınız pasif durumda");

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Başarısız giriş: {Email}", request.Email);
                    await Task.Delay(Random.Shared.Next(100, 500));
                    return ApiResponse<AuthResponse>.FailResult("Email veya şifre hatalı");
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var userDto = _mapper.Map<UserDto>(user);
                _logger.LogInformation("Başarılı giriş: {Email}", request.Email);

                return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse { Token = token, User = userDto }, "Giriş başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş sırasında hata");
                return ApiResponse<AuthResponse>.FailResult("Giriş işlemi başarısız");
            }
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(int userId)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.IsActive)
                    return ApiResponse<AuthResponse>.FailResult("Kullanıcı bulunamadı");

                var token = GenerateJwtToken(user);
                var userDto = _mapper.Map<UserDto>(user);
                return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse { Token = token, User = userDto }, "Token yenilendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme hatası");
                return ApiResponse<AuthResponse>.FailResult("Token yenileme başarısız");
            }
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return ApiResponse<bool>.FailResult("Kullanıcı bulunamadı");

                if (!VerifyPassword(oldPassword, user.PasswordHash))
                    return ApiResponse<bool>.FailResult("Mevcut şifre hatalı");

                if (!IsPasswordStrong(newPassword))
                    return ApiResponse<bool>.FailResult("Şifre en az 8 karakter, 1 büyük harf, 1 küçük harf ve 1 rakam içermelidir");

                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Şifre değiştirildi: UserId={UserId}", userId);

                return ApiResponse<bool>.SuccessResult(true, "Şifre başarıyla değiştirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirme hatası");
                return ApiResponse<bool>.FailResult("Şifre değiştirme başarısız");
            }
        }

        public async Task<ApiResponse<bool>> UpdateUserRoleAsync(int userId, string role)
        {
            try
            {
                if (role != "Admin" && role != "User")
                    return ApiResponse<bool>.FailResult("Geçersiz rol");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return ApiResponse<bool>.FailResult("Kullanıcı bulunamadı");

                user.Role = role;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Rol güncellendi: UserId={UserId}, Role={Role}", userId, role);

                return ApiResponse<bool>.SuccessResult(true, $"Kullanıcı rolü {role} olarak güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncelleme hatası");
                return ApiResponse<bool>.FailResult("Rol güncellenemedi");
            }
        }

        public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync(int page, int pageSize, string? search)
        {
            try
            {
                var query = _context.Users.Include(u => u.Subscription).AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
                }

                var totalCount = await query.CountAsync();
                var users = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);

                return ApiResponse<List<UserDto>>.SuccessResult(userDtos, $"Toplam {totalCount} kullanıcı bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar listeleme hatası");
                return ApiResponse<List<UserDto>>.FailResult("Kullanıcılar listelenemedi");
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Subscription).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return ApiResponse<UserDto>.FailResult("Kullanıcı bulunamadı");

                var userDto = _mapper.Map<UserDto>(user);
                return ApiResponse<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgisi alma hatası");
                return ApiResponse<UserDto>.FailResult("Kullanıcı bilgisi alınamadı");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key bulunamadı");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("HasActiveSubscription", (user.Subscription?.IsActive() ?? false).ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;
            return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
        }
    }
}