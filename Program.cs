using FluentValidation;
using FluentValidation.AspNetCore;
using Hesapix.Data;
using Hesapix.Mapping;
using Hesapix.Middleware;
using Hesapix.Models.Entities;
using Hesapix.Services.Implementations;
using Hesapix.Services.Interfaces;
using Hesapix.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Database with connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });

    // Performance optimizations
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
}, ServiceLifetime.Scoped);

// Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // MB
    options.CompactionPercentage = 0.25;
});

// AutoMapper - MappingProfile ile
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<MappingProfile>();
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// HttpClient Factory
builder.Services.AddHttpClient();

// Services Registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStokService, StokService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IMobilePaymentService, MobilePaymentService>();

// Background Services
builder.Services.AddHostedService<SubscriptionExpirationBackgroundService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hesapix API",
        Version = "v1",
        Description = "Hesapix - İşletme Yönetim Sistemi API",
        Contact = new OpenApiContact
        {
            Name = "Hesapix",
            Email = "support@hesapix.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hesapix API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseSerilogRequestLogging();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SubscriptionMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health");

// Database Migration ve Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Migration uygula
        if (context.Database.GetPendingMigrations().Any())
        {
            Log.Information("Applying database migrations...");
            context.Database.Migrate();
        }

        // Admin kullanıcı yoksa oluştur
        if (!context.Users.Any(u => u.Role == Hesapix.Models.Enums.UserRole.Admin))
        {
            Log.Information("Creating default admin user...");
            var admin = new User
            {
                Email = "admin@hesapix.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CompanyName = "Hesapix Admin",
                Role = Hesapix.Models.Enums.UserRole.Admin,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(admin);
            context.SaveChanges();
            Log.Information("Admin user created: admin@hesapix.com / Admin123!");
        }

        // Test kullanıcı
        if (!context.Users.Any(u => u.Email == "user@hesapix.com"))
        {
            Log.Information("Creating default user...");
            var user = new User
            {
                Email = "user@hesapix.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CompanyName = "Test Company",
                Role = Hesapix.Models.Enums.UserRole.User,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.SaveChanges();

            // Test kullanıcıya aktif subscription
            var subscription = new Subscription
            {
                UserId = user.Id,
                PlanType = Hesapix.Models.Enums.SubscriptionPlanType.Monthly,
                Status = Hesapix.Models.Enums.SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Price = 299,
                FinalPrice = 299,
                IsTrial = false,
                AutoRenew = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Subscriptions.Add(subscription);
            context.SaveChanges();

            Log.Information("User created: user@hesapix.com / Admin123! with active subscription");
        }

        // Subscription settings
        if (!context.SubscriptionSettings.Any())
        {
            context.SubscriptionSettings.Add(new SubscriptionSettings
            {
                TrialEnabled = true,
                TrialDurationDays = 14,
                MonthlyPrice = 299,
                YearlyPrice = 2990,
                CampaignEnabled = false,
                CampaignDiscountPercent = 0,
                UpdatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
            Log.Information("Default subscription settings created");
        }

        Log.Information("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
        throw;
    }
}

Log.Information("Starting Hesapix API...");
app.Run();
Log.Information("Hesapix API stopped");
Log.CloseAndFlush();

// Background Service for Subscription Expiration
public class SubscriptionExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionExpirationBackgroundService> _logger;

    public SubscriptionExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Expiration Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                await subscriptionService.ProcessExpiredSubscriptionsAsync();

                // Her 1 saatte bir çalış
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Subscription Expiration Background Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Subscription Expiration Background Service stopped");
    }
}