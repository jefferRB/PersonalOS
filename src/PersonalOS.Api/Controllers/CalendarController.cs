using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Calendar;
using PersonalOS.Application.Calendar;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// The calendar of the authenticated account: what is planned, when it repeats, and what was done.
/// </summary>
/// <remarks>
/// <para>
/// Reads are plain requests. Unsafe methods additionally require a valid antiforgery token through
/// the globally registered filter, and are rate limited by the <c>calendar</c> policy.
/// </para>
/// <para>
/// Every response is <c>no-store</c> through <see cref="DailyApiControllerBase"/>. A calendar
/// describes where somebody will be and when, so no proxy or shared browser may keep a copy.
/// </para>
/// </remarks>
[Route("api/calendar")]
public sealed class CalendarController(
    CalendarService calendarService,
    ILogger<CalendarController> logger) : DailyApiControllerBase
{
    /// <summary>
    /// Reads the summaries the month grid needs.
    /// </summary>
    /// <param name="year">Year being shown.</param>
    /// <param name="month">Month being shown, from 1 to 12.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The response carries counts and kind indicators, never titles or descriptions. A month view
    /// shows neither, and sending them would put a grid's worth of private text on the wire.
    /// </remarks>
    [HttpGet("month")]
    [ProducesResponseType<CalendarMonthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CalendarMonthResponse>> GetMonth(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await calendarService.GetMonthAsync(userId, year, month, cancellationToken);

        return ToActionResult(result, CalendarMonthResponse.FromRecord);
    }

    /// <summary>
    /// Reads everything on one local calendar day.
    /// </summary>
    /// <param name="date">
    /// Local calendar day, as <c>yyyy-MM-dd</c>. When omitted, the server uses the account's current
    /// local day, decided from the application clock and the saved time zone rather than from the
    /// browser.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("day")]
    [ProducesResponseType<CalendarDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CalendarDayResponse>> GetDay(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var day = await calendarService.GetDayAsync(userId, date, cancellationToken);

        return Ok(CalendarDayResponse.FromRecord(day));
    }

    /// <summary>
    /// Reads the next seven local days.
    /// </summary>
    /// <param name="from">
    /// First local calendar day, as <c>yyyy-MM-dd</c>. When omitted, the server starts from the
    /// account's current local day.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The window is bounded at seven days and returns everything inside it, each occurrence
    /// carrying the server's own <c>isImportant</c> answer. That is what lets the section's filters
    /// run on the client instead of costing a request per click.
    /// </remarks>
    [HttpGet("upcoming")]
    [ProducesResponseType<UpcomingWeekResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpcomingWeekResponse>> GetUpcoming(
        [FromQuery] DateOnly? from,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var week = await calendarService.GetUpcomingAsync(userId, from, cancellationToken);

        return Ok(UpcomingWeekResponse.FromRecord(week));
    }

    /// <summary>
    /// Reads one item owned by the authenticated account.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("items/{id:guid}")]
    [ProducesResponseType<PlanningItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanningItemResponse>> GetItem(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await calendarService.GetItemAsync(userId, id, cancellationToken);

        return ToActionResult(result, PlanningItemResponse.FromRecord);
    }

    /// <summary>
    /// Creates an item owned by the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("items")]
    [EnableRateLimiting("calendar")]
    [ProducesResponseType<PlanningItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PlanningItemResponse>> Create(
        SavePlanningItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await calendarService.CreateAsync(
            userId,
            request.ToInput(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result, PlanningItemResponse.FromRecord);
        }

        // The title and the description are private content and are deliberately not logged.
        logger.LogInformation(
            "Calendar item {ItemId} created for user {UserId}.",
            result.Value!.Id,
            userId);

        return CreatedAtAction(
            nameof(GetItem),
            new { id = result.Value.Id },
            PlanningItemResponse.FromRecord(result.Value));
    }

    /// <summary>
    /// Edits an item owned by the authenticated account.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Content and times belong to the whole series. Once a day has been completed or cancelled the
    /// repetition itself is frozen, and the response's <c>isRecurrencePatternLocked</c> flag tells
    /// the editor to disable those controls before the user tries.
    /// </remarks>
    [HttpPut("items/{id:guid}")]
    [EnableRateLimiting("calendar")]
    [ProducesResponseType<PlanningItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanningItemResponse>> Update(
        Guid id,
        SavePlanningItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await calendarService.UpdateAsync(
            userId,
            id,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, PlanningItemResponse.FromRecord);
    }

    /// <summary>
    /// Deletes an item, and with it the whole series and every decision recorded against it.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// There is no "delete one occurrence". Cancelling a single day is what that means, and it keeps
    /// the record of the decision instead of pretending the day never existed.
    /// </remarks>
    [HttpDelete("items/{id:guid}")]
    [EnableRateLimiting("calendar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await calendarService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            return NotFoundProblem();
        }

        logger.LogInformation(
            "Calendar item {ItemId} deleted for user {UserId}.",
            id,
            userId);

        return NoContent();
    }

    /// <summary>
    /// Records what the user decided about one occurrence.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="occurrenceDate">Local calendar day, as <c>yyyy-MM-dd</c>.</param>
    /// <param name="request">The decision being recorded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The call is idempotent, so a double click or a retried request is harmless. A row is written
    /// only when the decision is something other than "planned", because the absence of a row
    /// already means exactly that.
    /// </remarks>
    [HttpPut("items/{id:guid}/occurrences/{occurrenceDate}/status")]
    [EnableRateLimiting("calendar")]
    [ProducesResponseType<CalendarOccurrenceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarOccurrenceResponse>> SetOccurrenceStatus(
        Guid id,
        DateOnly occurrenceDate,
        SetOccurrenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await calendarService.SetOccurrenceStatusAsync(
            userId,
            id,
            occurrenceDate,
            request.Status,
            cancellationToken);

        return ToActionResult(result, CalendarOccurrenceResponse.FromRecord);
    }
}
