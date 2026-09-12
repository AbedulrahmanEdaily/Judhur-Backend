using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Exceptions;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var detail = environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred. If the problem persists, contact support with the request id below.";
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = detail,
            },
        });
    }
}