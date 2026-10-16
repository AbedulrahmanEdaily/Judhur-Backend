using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Judhur.Api.Exceptions;

public sealed class DbUpdateExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<DbUpdateExceptionHandler> logger) : IExceptionHandler
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for request {RequestId} {Method} {Path}", httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Title = "تعارض في البيانات",
                    Detail = "تم تعديل هذه البيانات من طلب آخر في الوقت نفسه، يرجى تحديث الصفحة والمحاولة مجددًا.",
                    Extensions = { ["code"] = "Database.ConcurrencyConflict" },
                },
            });
        }
        if (exception is not DbUpdateException { InnerException: SqlException sqlException }
            || sqlException.Number is not (UniqueIndexViolation or UniqueConstraintViolation))
        {
            return false;
        }
        logger.LogWarning("Unique index violation {SqlErrorNumber} for request {RequestId} {Method} {Path}", sqlException.Number, httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "تعارض في البيانات",
                Detail = "هذا الإجراء يتعارض مع بيانات موجودة، ربما قام أحد بتنفيذ الإجراء نفسه مسبقًا.",
                Extensions = { ["code"] = "Database.DuplicateEntry" },
            },
        });
    }
}
