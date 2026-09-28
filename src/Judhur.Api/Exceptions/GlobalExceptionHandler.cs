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
            : "حدث خطأ غير متوقع. إذا تكررت المشكلة، يرجى التواصل مع الدعم مع ذكر رقم الطلب أدناه.";
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "حدث خطأ غير متوقع",
                Detail = detail,
            },
        });
    }
}