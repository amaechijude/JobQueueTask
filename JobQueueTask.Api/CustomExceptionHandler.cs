using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobQueueTask.Api;

public sealed class CustomExceptionHanler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problem = new ProblemDetails
        {
            Type = "Bad request",
            Status = StatusCodes.Status400BadRequest,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Detail = exception.Message,
        };

        ProblemDetailsContext context = new()
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        };
        return await problemDetailsService.TryWriteAsync(context);
    }
}
