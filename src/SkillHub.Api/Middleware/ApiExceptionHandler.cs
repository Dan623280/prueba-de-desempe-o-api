using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillHub.Application.Exceptions;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Api.Middleware;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            AuthenticationException => StatusCodes.Status401Unauthorized,
            ValidationException or BadHttpRequestException or InvalidDataException => StatusCodes.Status400BadRequest,
            BusinessRuleException => StatusCodes.Status409Conflict,
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        var message = status == 500 ? "An unexpected error occurred. Please try again later."
            : exception is DbUpdateException ? "A record with these unique values already exists." : exception.Message;
        if (status == 500)
        {
            _logger.LogError(exception, "Unhandled API error. Trace: {TraceId}", context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}
