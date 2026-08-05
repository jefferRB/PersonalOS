using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Study;
using PersonalOS.Application.Study;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Study projects and study sessions owned by the authenticated account.
/// </summary>
/// <remarks>
/// Resource links are metadata. The server validates that a link is an absolute <c>http</c> or
/// <c>https</c> address, then stores the string. It never requests the address, never renders
/// what the address returns, and never accepts an uploaded file.
/// </remarks>
[Route("api/study")]
public sealed class StudyController(StudyService studyService) : DailyApiControllerBase
{
    /// <summary>
    /// Reads the projects of the authenticated account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("projects")]
    [ProducesResponseType<IReadOnlyList<StudyProjectResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<StudyProjectResponse>>> GetProjects(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var projects = await studyService.GetProjectsAsync(userId, cancellationToken);

        return Ok(projects.Select(StudyProjectResponse.FromRecord).ToList());
    }

    /// <summary>
    /// Creates a project owned by the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("projects")]
    [EnableRateLimiting("study")]
    [ProducesResponseType<StudyProjectResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StudyProjectResponse>> CreateProject(
        SaveStudyProjectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await studyService.CreateProjectAsync(
            userId,
            request.ToInput(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result, StudyProjectResponse.FromRecord);
        }

        return Created(
            $"/api/study/projects/{result.Value!.Id}",
            StudyProjectResponse.FromRecord(result.Value));
    }

    /// <summary>
    /// Edits a project owned by the authenticated account.
    /// </summary>
    /// <param name="id">Project identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("projects/{id:guid}")]
    [EnableRateLimiting("study")]
    [ProducesResponseType<StudyProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyProjectResponse>> UpdateProject(
        Guid id,
        SaveStudyProjectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await studyService.UpdateProjectAsync(
            userId,
            id,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, StudyProjectResponse.FromRecord);
    }

    /// <summary>
    /// Reads the sessions recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<StudySessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<StudySessionResponse>>> GetSessions(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await studyService.GetSessionsAsync(userId, from, to, cancellationToken);

        return ToActionResult(
            result,
            sessions => (IReadOnlyList<StudySessionResponse>)
                [.. sessions.Select(StudySessionResponse.FromRecord)]);
    }

    /// <summary>
    /// Records a study session owned by the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("sessions")]
    [EnableRateLimiting("study")]
    [ProducesResponseType<StudySessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StudySessionResponse>> CreateSession(
        SaveStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await studyService.CreateSessionAsync(
            userId,
            request.ToInput(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result, StudySessionResponse.FromRecord);
        }

        return Created(
            $"/api/study/sessions/{result.Value!.Id}",
            StudySessionResponse.FromRecord(result.Value));
    }

    /// <summary>
    /// Edits a study session owned by the authenticated account.
    /// </summary>
    /// <param name="id">Session identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("sessions/{id:guid}")]
    [EnableRateLimiting("study")]
    [ProducesResponseType<StudySessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudySessionResponse>> UpdateSession(
        Guid id,
        SaveStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await studyService.UpdateSessionAsync(
            userId,
            id,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, StudySessionResponse.FromRecord);
    }

    /// <summary>
    /// Deletes a study session owned by the authenticated account.
    /// </summary>
    /// <param name="id">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("sessions/{id:guid}")]
    [EnableRateLimiting("study")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await studyService.DeleteSessionAsync(userId, id, cancellationToken);

        return deleted ? NoContent() : NotFoundProblem();
    }
}
