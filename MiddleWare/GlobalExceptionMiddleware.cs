/*
using System.Net;
using System.Text.Json;
using Hesapix.Models.Common;

namespace Hesapix.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
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
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request"
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = exception.Message;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Errors = validationEx.Errors.ToList();
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = _environment.IsDevelopment()
                        ? exception.Message
                        : "An internal server error occurred";
                    break;
            }

            if (_environment.IsDevelopment())
            {
                response.Data = new
                {
                    exception.Message,
                    exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(IEnumerable<string> errors) : base("Validation failed")
        {
            Errors = errors;
        }

        public ValidationException(string error) : base("Validation failed")
        {
            Errors = new[] { error };
        }
    }
}
*/ //BURADA ESKİ KODLAR VAR
using System.Net;
using System.Text.Json;
using Hesapix.Models.Common;

namespace Hesapix.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
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
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request"
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = exception.Message;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Errors = validationEx.Errors.ToList();
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = _environment.IsDevelopment()
                        ? exception.Message
                        : "An internal server error occurred";
                    break;
            }

            if (_environment.IsDevelopment())
            {
                response.Data = new
                {
                    exception.Message,
                    exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    // ValidationException tipi
    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(IEnumerable<string> errors) : base("Validation failed")
        {
            Errors = errors;
        }

        public ValidationException(string error) : base("Validation failed")
        {
            Errors = new[] { error };
        }
    }
}