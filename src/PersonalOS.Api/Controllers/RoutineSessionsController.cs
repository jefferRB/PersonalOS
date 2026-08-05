using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Routines;
using PersonalOS.Application.Routines;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Progress recorded against a routine session.
/// </summary>
/// <remarks>
/// Sessions live under their own route because they are edited independently of the routine that
/// produced them: changing next week's plan must not touch what was lifted this morning.
/// </remarks>
[Route("api/routine-sessions")]
public sealed class RoutineSessionsController(RoutineService routineService)
    : DailyApiControllerBase
{
    /// <summary>
    /// Reads one session owned by the authenticated account.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType<RoutineSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutineSessionResponse>> Get(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await routineService.GetSessionAsync(userId, sessionId, cancellationToken);

        return ToActionResult(result, RoutineSessionResponse.FromRecord);
    }

    /// <summary>
    /// Saves progress on a session owned by the authenticated account.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The same call saves partial progress and completes the routine, so leaving the screen
    /// halfway through never loses what was already entered.
    /// </remarks>
    [HttpPut("{sessionId:guid}")]
    [EnableRateLimiting("routines")]
    [ProducesResponseType<RoutineSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutineSessionResponse>> Save(
        Guid sessionId,
        SaveRoutineSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await routineService.SaveSessionAsync(
            userId,
            sessionId,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, RoutineSessionResponse.FromRecord);
    }
}
