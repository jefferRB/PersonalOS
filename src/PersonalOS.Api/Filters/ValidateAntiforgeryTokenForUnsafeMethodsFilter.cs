using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PersonalOS.Api.Filters;

public sealed class ValidateAntiforgeryTokenForUnsafeMethodsFilter(
    IAntiforgery antiforgery,
    ILogger<ValidateAntiforgeryTokenForUnsafeMethodsFilter> logger) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        if (!UnsafeMethods.Contains(httpContext.Request.Method))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            logger.LogWarning(
                "Antiforgery validation failed for {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path.Value);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request verification failed.",
                Detail = "The request could not be verified.",
                Instance = httpContext.Request.Path,
            };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" },
            };
        }
    }
}
