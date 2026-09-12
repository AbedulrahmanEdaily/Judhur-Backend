using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Judhur.Api.Exceptions;
public class DbUpdateExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException { InnerException: SqlException sqlException }
            || sqlException.Number is not (UniqueIndexViolation or UniqueConstraintViolation))
        {
            return false;
        }
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "Conflict",
                Detail = "This action conflicts with existing data. Someone may have already done the same thing.",
            },
        });
    }
}
