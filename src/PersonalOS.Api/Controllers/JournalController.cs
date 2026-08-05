using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Journal;
using PersonalOS.Application.Journal;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// The daily reflection of the authenticated account.
/// </summary>
/// <remarks>
/// <para>
/// This is the most sensitive endpoint in PersonalOS. The controls that follow from that are
/// deliberate and are all visible here:
/// </para>
/// <list type="bullet">
/// <item><description>the day comes from the route and the account from the authentication cookie,
/// so neither can be selected through the request body;</description></item>
/// <item><description>every response carries <c>Cache-Control: no-store</c>, inherited from the
/// base controller, so no proxy or shared browser keeps a copy;</description></item>
/// <item><description>no log statement in this file, in the service, or in the store writes any
/// section of the reflection;</description></item>
/// <item><description>the text never travels in a query string, so it cannot end up in a server
/// access log or a browser history entry.</description></item>
/// </list>
/// </remarks>
[Route("api/journal")]
public sealed class JournalController(
    JournalService journalService,
    ILogger<JournalController> logger) : DailyApiControllerBase
{
    /// <summary>
    /// Reads the entry of one local calendar day.
    /// </summary>
    /// <param name="date">Local calendar day, as <c>yyyy-MM-dd</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entry, or an empty one when the day has not been written about.</returns>
    [HttpGet("{date}")]
    [ProducesResponseType<JournalEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JournalEntryResponse>> Get(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var entry = await journalService.GetAsync(userId, date, cancellationToken);

        return Ok(JournalEntryResponse.FromRecord(entry));
    }

    /// <summary>
    /// Creates or updates the entry of one local calendar day.
    /// </summary>
    /// <param name="date">Local calendar day, as <c>yyyy-MM-dd</c>.</param>
    /// <param name="request">Submitted sections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Saving the same day twice updates the existing entry, so repeated saves while writing
    /// never accumulate duplicate days.
    /// </remarks>
    [HttpPut("{date}")]
    [EnableRateLimiting("journal")]
    [ProducesResponseType<JournalEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JournalEntryResponse>> Save(
        DateOnly date,
        SaveJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await journalService.SaveAsync(
            userId,
            date,
            request.ToInput(),
            cancellationToken);

        // The account, the date, and the outcome are safe to log. No section of the reflection is
        // logged, here or anywhere else.
        logger.LogInformation(
            "Journal entry saved for user {UserId} on {LocalDate} with status {Status}.",
            userId,
            date,
            result.Status);

        return ToActionResult(result, JournalEntryResponse.FromRecord);
    }
}
