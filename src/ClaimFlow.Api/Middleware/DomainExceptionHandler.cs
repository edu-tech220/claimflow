using ClaimFlow.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Middleware;

/// <summary>
/// Maps business-rule violations, concurrency conflicts and missing-entity lookups to
/// RFC 7807 problem responses instead of letting them bubble up as generic 500s.
/// </summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status409Conflict, "Business rule violation"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by someone else. Please reload and try again."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}
