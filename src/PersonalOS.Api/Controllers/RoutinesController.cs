using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Routines;
using PersonalOS.Application.Routines;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Routines owned by the authenticated account.
/// </summary>
[Route("api/routines")]
public sealed class RoutinesController(
    RoutineService routineService,
    ILogger<RoutinesController> logger) : DailyApiControllerBase
{
    /// <summary>
    /// Reads the routines of the authenticated account.
    /// </summary>
    /// <param name="activeOnly">Whether to skip deactivated routines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoutineTemplateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<RoutineTemplateResponse>>> GetAll(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var routines = await routineService.GetTemplatesAsync(
            userId,
            activeOnly,
            cancellationToken);

        return Ok(routines.Select(RoutineTemplateResponse.FromRecord).ToList());
    }

    /// <summary>
    /// Calculates which routines apply inside an inclusive local-date range.
    /// </summary>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Occurrences are calculated from the stored rules. No row exists for a day until the user
    /// actually starts the routine on it.
    /// </remarks>
    [HttpGet("occurrences")]
    [ProducesResponseType<IReadOnlyList<RoutineOccurrenceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<RoutineOccurrenceResponse>>> GetOccurrences(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (to < from)
        {
            return ValidationProblemFromErrors(new Dictionary<string, string[]>
            {
                ["to"] = ["The end of the range cannot be before its start."],
            });
        }

        if (to.DayNumber - from.DayNumber > MaxOccurrenceRangeDays)
        {
            return ValidationProblemFromErrors(new Dictionary<string, string[]>
            {
                ["to"] = [$"A range may cover at most {MaxOccurrenceRangeDays} days."],
            });
        }

        var occurrences = await routineService.GetOccurrencesAsync(
            userId,
            from,
            to,
            cancellationToken);

        return Ok(occurrences.Select(RoutineOccurrenceResponse.FromRecord).ToList());
    }

    /// <summary>
    /// Reads one routine owned by the authenticated account.
    /// </summary>
    /// <param name="id">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RoutineTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutineTemplateResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await routineService.GetTemplateAsync(userId, id, cancellationToken);

        return ToActionResult(result, RoutineTemplateResponse.FromRecord);
    }

    /// <summary>
    /// Creates a routine owned by the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [EnableRateLimiting("routines")]
    [ProducesResponseType<RoutineTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoutineTemplateResponse>> Create(
        SaveRoutineRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await routineService.CreateAsync(userId, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result, RoutineTemplateResponse.FromRecord);
        }

        logger.LogInformation(
            "Routine {RoutineId} created for user {UserId} with {StepCount} steps.",
            result.Value!.Id,
            userId,
            result.Value.Steps.Count);

        return CreatedAtAction(
            nameof(Get),
            new { id = result.Value.Id },
            RoutineTemplateResponse.FromRecord(result.Value));
    }

    /// <summary>
    /// Edits a routine owned by the authenticated account.
    /// </summary>
    /// <param name="id">Routine identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("routines")]
    [ProducesResponseType<RoutineTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutineTemplateResponse>> Update(
        Guid id,
        SaveRoutineRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await routineService.UpdateAsync(
            userId,
            id,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, RoutineTemplateResponse.FromRecord);
    }

    /// <summary>
    /// Deletes a routine owned by the authenticated account.
    /// </summary>
    /// <param name="id">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Deleting a routine also removes the sessions recorded against it. Deactivating is offered
    /// in the interface as the way to stop a routine while keeping its history.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("routines")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await routineService.DeleteAsync(userId, id, cancellationToken);

        return deleted ? NoContent() : NotFoundProblem();
    }

    /// <summary>
    /// Starts, or returns, the session of a routine on one local calendar day.
    /// </summary>
    /// <param name="id">Routine identifier.</param>
    /// <param name="request">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:guid}/sessions")]
    [EnableRateLimiting("routines")]
    [ProducesResponseType<RoutineSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutineSessionResponse>> StartSession(
        Guid id,
        StartRoutineSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (request.LocalDate is null)
        {
            return ValidationProblemFromErrors(new Dictionary<string, string[]>
            {
                ["localDate"] = ["Choose the day this session belongs to."],
            });
        }

        var result = await routineService.StartSessionAsync(
            userId,
            id,
            request.LocalDate.Value,
            cancellationToken);

        return ToActionResult(result, RoutineSessionResponse.FromRecord);
    }

    /// <summary>Largest occurrence range a single query may cover, in days.</summary>
    private const int MaxOccurrenceRangeDays = 400;
}
