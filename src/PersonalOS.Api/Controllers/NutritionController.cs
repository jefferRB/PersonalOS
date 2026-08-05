using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Nutrition;
using PersonalOS.Application.Nutrition;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Daily calorie totals and the target the authenticated account chose.
/// </summary>
/// <remarks>
/// The endpoints report arithmetic. They never propose a target, never label a value, and never
/// return advice: PersonalOS is not a medical product.
/// </remarks>
[Route("api/nutrition")]
public sealed class NutritionController(NutritionService nutritionService)
    : DailyApiControllerBase
{
    /// <summary>
    /// Reads one local day, with its meals and totals.
    /// </summary>
    /// <param name="date">Local calendar day, as <c>yyyy-MM-dd</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("day")]
    [ProducesResponseType<NutritionDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionDayResponse>> GetDay(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var day = await nutritionService.GetDayAsync(userId, date, cancellationToken);

        return Ok(NutritionDayResponse.FromRecord(day));
    }

    /// <summary>
    /// Reads the daily targets of the authenticated account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("goal")]
    [ProducesResponseType<NutritionGoalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionGoalResponse>> GetGoal(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var goal = await nutritionService.GetGoalAsync(userId, cancellationToken);

        return Ok(NutritionGoalResponse.FromRecord(goal));
    }

    /// <summary>
    /// Saves the daily targets of the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("goal")]
    [EnableRateLimiting("nutrition")]
    [ProducesResponseType<NutritionGoalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionGoalResponse>> SaveGoal(
        SaveNutritionGoalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await nutritionService.SaveGoalAsync(
            userId,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, NutritionGoalResponse.FromRecord);
    }
}
