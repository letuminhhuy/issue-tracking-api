using System.Net;
using System.Text.Json;

namespace IssueTrackingAPI.Common;

/// <summary>
/// Middleware bắt các ApiException nghiệp vụ và trả về JSON response gọn gàng
/// kèm status code phù hợp, thay vì để lỗi 500 mặc định.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ForbiddenException => HttpStatusCode.Forbidden,
                BadRequestException => HttpStatusCode.BadRequest,
                ConflictException => HttpStatusCode.Conflict,
                UnauthorizedAppException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = new
            {
                status = (int)statusCode,
                message = statusCode == HttpStatusCode.InternalServerError
                    ? "Đã có lỗi xảy ra ở server."
                    : ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
