using Hesapix.Models.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log exception
        Log.Error(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            context.Request.Path,
            context.Request.Method);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiResponse
        {
            Success = false,
            Message = "Bir hata oluştu",
            Errors = new List<string>()
        };

        // Development'ta detaylı hata mesajı
        if (_environment.IsDevelopment())
        {
            response.Message = exception.Message;
            response.Errors = new List<string>
            {
                exception.StackTrace ?? "Stack trace not available"
            };
        }
        else
        {
            // Production'da güvenlik için genel mesaj
            response.Message = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            response.Errors = new List<string> { "INTERNAL_SERVER_ERROR" };
        }

        // Özel exception türlerine göre özelleştirme
        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Yetkiniz bulunmamaktadır";
                response.Errors = new List<string> { "UNAUTHORIZED" };
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = argEx.Message;
                response.Errors = new List<string> { "INVALID_ARGUMENT" };
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Kayıt bulunamadı";
                response.Errors = new List<string> { "NOT_FOUND" };
                break;

            case InvalidOperationException invEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = invEx.Message;
                response.Errors = new List<string> { "INVALID_OPERATION" };
                break;

            case DbUpdateException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = "Veritabanı işlemi başarısız oldu";
                response.Errors = new List<string> { "DATABASE_ERROR" };
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var result = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(result);
    }
}

// Custom exception için using ekle
