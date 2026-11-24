using Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api;

/// <summary>
/// Turns known exceptions into ProblemDetails (RFC 9457). Anything else falls through to
/// UseExceptionHandler, which answers 500 without a stack trace.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails? problem = exception switch
        {
            // a broken business rule is the client's conflict with the current state: 409
            DomainException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The request conflicts with a business rule.",
                Detail = ex.Message
            },

            // empty body, broken JSON, "quantity":"abc": thrown (instead of a plain 400) in Development
            BadHttpRequestException ex => new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = "The request could not be read.",
                Detail = "The body is missing or is not valid JSON of the expected shape."
            },

            _ => null
        };

        if (problem is null)
            return false;

        context.Response.StatusCode = problem.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = null            // never expose the exception itself to the client
        });
    }
}
