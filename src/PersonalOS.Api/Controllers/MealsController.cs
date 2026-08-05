using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Nutrition;
using PersonalOS.Application.Nutrition;

namespace PersonalOS.Api.Controllers;

/// <summary>
/// Meals recorded by the authenticated account.
/// </summary>
/// <remarks>
/// PersonalOS holds no food database and contacts no external nutrition service. Every value
/// stored here is a value the user typed.
/// </remarks>
[Route("api/meals")]
public sealed class MealsController(NutritionService nutritionService) : DailyApiControllerBase
{
    /// <summary>
    /// Records a meal owned by the authenticated account.
    /// </summary>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [EnableRateLimiting("nutrition")]
    [ProducesResponseType<MealEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MealEntryResponse>> Create(
        SaveMealRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await nutritionService.CreateMealAsync(
            userId,
            request.ToInput(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result, MealEntryResponse.FromRecord);
        }

        return Created(
            $"/api/meals/{result.Value!.Id}",
            MealEntryResponse.FromRecord(result.Value));
    }

    /// <summary>
    /// Edits a meal owned by the authenticated account.
    /// </summary>
    /// <param name="id">Meal identifier.</param>
    /// <param name="request">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("nutrition")]
    [ProducesResponseType<MealEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealEntryResponse>> Update(
        Guid id,
        SaveMealRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await nutritionService.UpdateMealAsync(
            userId,
            id,
            request.ToInput(),
            cancellationToken);

        return ToActionResult(result, MealEntryResponse.FromRecord);
    }

    /// <summary>
    /// Deletes a meal owned by the authenticated account.
    /// </summary>
    /// <param name="id">Meal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("nutrition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await nutritionService.DeleteMealAsync(userId, id, cancellationToken);

        return deleted ? NoContent() : NotFoundProblem();
    }
}
