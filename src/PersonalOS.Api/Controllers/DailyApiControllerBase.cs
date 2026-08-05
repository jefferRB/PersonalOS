using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalOS.Api.Security;
using PersonalOS.Application.Common;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Shared behaviour for the daily module controllers.
/// </summary>
/// <remarks>
/// <para>
/// Every daily endpoint requires authentication and derives its account identifier from the
/// authentication cookie. Putting that in one place means a new endpoint cannot forget it.
/// </para>
/// <para>
/// The daily modules hold personal data, so every response is marked <c>no-store</c>. A shared
/// browser or a proxy must not keep a copy of what somebody planned, ate, or studied.
/// </para>
/// </remarks>
[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class DailyApiControllerBase : ControllerBase
{
    /// <summary>
    /// Reads the authenticated account identifier from the request principal.
    /// </summary>
    /// <param name="userId">Account identifier when the principal carries one.</param>
    /// <returns><see langword="true"/> when the principal carries a usable identifier.</returns>
    protected bool TryGetUserId(out Guid userId) => User.TryGetUserId(out userId);

    /// <summary>
    /// Turns an application result into an HTTP response.
    /// </summary>
    /// <typeparam name="TValue">Type the operation produced.</typeparam>
    /// <typeparam name="TResponse">Public contract type.</typeparam>
    /// <param name="result">Application result.</param>
    /// <param name="toResponse">Projection onto the public contract.</param>
    /// <remarks>
    /// A resource that belongs to another account produces 404 rather than 403. Answering
    /// "forbidden" would confirm that the identifier names something real, which is information
    /// the caller has no right to.
    /// </remarks>
    protected ActionResult<TResponse> ToActionResult<TValue, TResponse>(
        OperationResult<TValue> result,
        Func<TValue, TResponse> toResponse)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(toResponse);

        return result.Status switch
        {
            OperationStatus.Succeeded => Ok(toResponse(result.Value!)),
            OperationStatus.Invalid => ValidationProblemFromErrors(result.ValidationErrors),
            _ => NotFoundProblem(),
        };
    }

    /// <summary>
    /// Produces a sanitized validation Problem Details response.
    /// </summary>
    /// <param name="errors">Messages keyed by the camel-case contract field name.</param>
    /// <remarks>
    /// The messages come from the application layer and never repeat what the user submitted, so
    /// a validation response cannot echo private text back through an error.
    /// </remarks>
    protected ActionResult ValidationProblemFromErrors(
        IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var problem = new ValidationProblemDetails(
            errors.ToDictionary(error => error.Key, error => error.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return BadRequest(problem);
    }

    /// <summary>
    /// Produces a Problem Details response for a resource this account cannot see.
    /// </summary>
    protected ActionResult NotFoundProblem() =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not found.",
            detail: "The requested item does not exist.",
            instance: HttpContext.Request.Path);
}
