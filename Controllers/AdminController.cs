using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;

    public AdminController(
        ApplicationDbContext context,
        IMapper mapper,
        ISubscriptionService subscriptionService,
        IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _subscriptionService = subscriptionService;
        _configuration = configuration;
    }

    // Kullanıcı Yönetimi
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.Email.Contains(searchTerm) || u.CompanyName.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<UserDto>
        {
            Items = _mapper.Map<List<UserDto>>(users),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResponse(result));
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserDetails(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse("Kullanıcı bulunamadı"));
        }

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
    }

    // Abonelik Yönetimi
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAllSubscriptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null)
    {
        var query = _context.Subscriptions
            .Include(s => s.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new
        {
            Items = subscriptions.Select(s => new
            {
                s.Id,
                s.UserId,
                UserEmail = s.User.Email,
                CompanyName = s.User.CompanyName,
                s.PlanType,
                s.Status,
                s.StartDate,
                s.EndDate,
                s.IsTrial,
                s.FinalPrice,
                s.PaymentGateway,
                s.CreatedAt
            }),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    // Sistem İstatistikleri
    [HttpGet("statistics")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow);
        var trialUsers = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Trial && s.EndDate > DateTime.UtcNow);
        var expiredSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Expired);

        var totalRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => (decimal?)s.FinalPrice) ?? 0;

        var monthlyRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
            .SumAsync(s => (decimal?)s.FinalPrice) ?? 0;

        var stats = new
        {
            TotalUsers = totalUsers,
            ActiveSubscriptions = activeSubscriptions,
            TrialUsers = trialUsers,
            ExpiredSubscriptions = expiredSubscriptions,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            ConversionRate = totalUsers > 0 ? (activeSubscriptions * 100.0 / totalUsers) : 0
        };

        return Ok(ApiResponse<object>.SuccessResponse(stats));
    }

    // Ayar Yönetimi
    [HttpGet("settings/subscription")]
    public IActionResult GetSubscriptionSettings()
    {
        var settings = _configuration.GetSection("SubscriptionSettings");
        var result = new
        {
            TrialEnabled = settings.GetValue<bool>("TrialEnabled"),
            TrialDurationDays = settings.GetValue<int>("TrialDurationDays"),
            MonthlyPrice = settings.GetValue<decimal>("MonthlyPrice"),
            YearlyPrice = settings.GetValue<decimal>("YearlyPrice"),
            CampaignEnabled = settings.GetValue<bool>("CampaignEnabled"),
            CampaignDiscountPercent = settings.GetValue<decimal>("CampaignDiscountPercent")
        };

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPut("settings/subscription")]
    public IActionResult UpdateSubscriptionSettings([FromBody] object settings)
    {
        // Bu endpoint için appsettings.json'ı runtime'da güncellemek gerekir
        // Production'da bunun yerine database'de ayarlar tutulmalı
        return Ok(ApiResponse.SuccessResponse("Ayarlar güncelleme özelliği eklenecek"));
    }

    // Manuel İşlemler
    [HttpPost("subscriptions/{subscriptionId}/cancel")]
    public async Task<IActionResult> CancelSubscription(int subscriptionId)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            return NotFound(ApiResponse.ErrorResponse("Abonelik bulunamadı"));
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Abonelik iptal edildi"));
    }

    [HttpPost("process-expired-subscriptions")]
    public async Task<IActionResult> ProcessExpiredSubscriptions()
    {
        await _subscriptionService.ProcessExpiredSubscriptionsAsync();
        return Ok(ApiResponse.SuccessResponse("Süre dolmuş abonelikler işlendi"));
    }

    // Kullanıcı Silme (Soft Delete)
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse.ErrorResponse("Kullanıcı bulunamadı"));
        }

        if (user.Role == UserRole.Admin)
        {
            return BadRequest(ApiResponse.ErrorResponse("Admin kullanıcılar silinemez"));
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Kullanıcı silindi"));
    }
}