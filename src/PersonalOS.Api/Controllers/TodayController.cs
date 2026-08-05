using Microsoft.AspNetCore.Mvc;
using PersonalOS.Api.Contracts.Today;
using PersonalOS.Application.Today;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// The integrated view of one local day.
/// </summary>
/// <remarks>
/// One request returns everything the Today screen needs, so the browser makes a single call
/// instead of six and the screen never renders half a day while the rest arrives.
/// </remarks>
[Route("api/today")]
public sealed class TodayController(TodayService todayService) : DailyApiControllerBase
{
    /// <summary>
    /// Reads the Today view of the authenticated account.
    /// </summary>
    /// <param name="date">
    /// Local calendar day to show, as <c>yyyy-MM-dd</c>. When omitted, the server uses the
    /// account's current local day, decided from the application clock and the saved time zone
    /// rather than from the browser.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<TodaySummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TodaySummaryResponse>> Get(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var summary = await todayService.GetAsync(userId, date, cancellationToken);

        return Ok(TodaySummaryResponse.FromRecord(summary));
    }
}
