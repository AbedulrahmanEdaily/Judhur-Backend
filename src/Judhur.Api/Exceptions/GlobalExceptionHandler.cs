using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Exceptions;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {RequestId} {Method} {Path} was cancelled by the client", httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path);
            return true;
        }

        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning("Bad request {RequestId} {Method} {Path}: {Reason}", httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path, badRequest.Message);
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                        ? "حجم الطلب أكبر من المسموح"
                        : "الطلب غير صالح",
                },
            });
        }

        logger.LogError(exception, "Unhandled exception for request {RequestId} {Method} {Path}", httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path);
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
