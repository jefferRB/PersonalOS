using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalOS.Api.Contracts.Time;
using PersonalOS.Api.Security;
using PersonalOS.Application.Time;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Time context of the authenticated account.
/// </summary>
/// <remarks>
/// The response is derived from the application clock and the account's persisted time zone, so
/// the local calendar date does not depend on the browser clock or on the browser time zone.
/// </remarks>
[ApiController]
[Route("api/time")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TimeController(TimeContextService timeContextService) : ControllerBase
{
    /// <summary>
    /// Returns the current instant in UTC and in the account's local time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("context")]
    [ProducesResponseType<TimeContextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimeContextResponse>> GetContext(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var localTime = await timeContextService.GetAsync(userId, cancellationToken);

        return Ok(TimeContextResponse.FromLocalTime(localTime));
    }
}
