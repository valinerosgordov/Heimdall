using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Heimdall.Api.Common;

/// <summary>Turns unhandled exceptions into RFC 9457 Problem Details. Malformed input maps to 400, everything else to 500.</summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Malformed request."),
            JsonException => (StatusCodes.Status400BadRequest, "Malformed request body."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Rejected {Method} {Path}: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problem = new ProblemDetails { Status = status, Title = title };
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
