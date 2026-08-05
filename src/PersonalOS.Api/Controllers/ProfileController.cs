using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PersonalOS.Api.Contracts.Profile;
using PersonalOS.Api.Security;
using PersonalOS.Application.Profile;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Profile of the authenticated account.
/// </summary>
/// <remarks>
/// Both endpoints require authentication, and the update endpoint additionally requires a valid
/// antiforgery token through the globally registered filter. Ownership is derived from the
/// authentication cookie, never from client input.
/// </remarks>
[ApiController]
[Route("api/profile")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(
    UserProfileService profileService,
    ILogger<ProfileController> logger) : ControllerBase
{
    /// <summary>
    /// Reads the profile of the authenticated account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var profile = await profileService.GetAsync(userId, cancellationToken);

        return profile is null
            ? Unauthorized()
            : Ok(UserProfileResponse.FromRecord(profile));
    }

    /// <summary>
    /// Updates the display name and time zone of the authenticated account.
    /// </summary>
    /// <param name="request">Requested values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut]
    [EnableRateLimiting("profile")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileResponse>> Update(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        UserProfileUpdateResult result;

        try
        {
            result = await profileService.UpdateAsync(
                userId,
                request.DisplayName,
                request.TimeZoneId,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The account row changed between reading and saving.
            logger.LogWarning("Profile update conflicted for user {UserId}.", userId);

            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Profile changed elsewhere.",
                detail: "The profile changed in another session. Reload and try again.");
        }

        switch (result.Status)
        {
            case UserProfileUpdateStatus.Saved:
                // The submitted values are intentionally not logged.
                logger.LogInformation("Profile updated for user {UserId}.", userId);

                return Ok(UserProfileResponse.FromRecord(result.Profile!));

            case UserProfileUpdateStatus.Invalid:
                logger.LogInformation(
                    "Profile update rejected for user {UserId} with {ErrorCount} validation errors.",
                    userId,
                    result.ValidationErrors.Count);

                return ValidationProblem(result.ValidationErrors);

            default:
                return Unauthorized();
        }
    }

    /// <summary>
    /// Updates how the day planner's timeline is shown to the authenticated account.
    /// </summary>
    /// <param name="request">Requested values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The calendar toolbar changes only the timeline, so it has its own endpoint rather than
    /// reusing the profile update. Sending a display name and a time zone along with a slot length
    /// would let the calendar overwrite settings it has no business touching.
    /// </remarks>
    [HttpPut("calendar-display")]
    [EnableRateLimiting("profile")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateCalendarDisplay(
        UpdateCalendarDisplayRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await profileService.UpdateCalendarDisplayAsync(
            userId,
            request.DayStartTime,
            request.DayEndTime,
            request.SlotMinutes,
            cancellationToken);

        switch (result.Status)
        {
            case UserProfileUpdateStatus.Saved:
                logger.LogInformation("Calendar display updated for user {UserId}.", userId);

                return Ok(UserProfileResponse.FromRecord(result.Profile!));

            case UserProfileUpdateStatus.Invalid:
                return ValidationProblem(result.ValidationErrors);

            default:
                return Unauthorized();
        }
    }

    private ActionResult ValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors.ToDictionary(
            error => error.Key,
            error => error.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return BadRequest(problem);
    }
}
