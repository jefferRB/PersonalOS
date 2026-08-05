using PersonalOS.Domain.Nutrition;

namespace PersonalOS.Application.Nutrition;

/// <summary>
/// Persistence port for the nutrition goal and meal entries of one account.
/// </summary>
public interface INutritionStore
{
    /// <summary>
    /// Reads the goal of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The goal, or <see langword="null"/> when the user has not chosen one.</returns>
    Task<NutritionGoal?> FindGoalAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or replaces the goal of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="dailyCalorieTarget">Calories the user is aiming for.</param>
    /// <param name="proteinTargetGrams">Optional protein target.</param>
    /// <param name="carbohydrateTargetGrams">Optional carbohydrate target.</param>
    /// <param name="fatTargetGrams">Optional fat target.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<NutritionGoal> SaveGoalAsync(
        Guid userId,
        int dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the meals recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<MealEntry>> GetMealsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one meal owned by an account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="mealId">Meal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<MealEntry?> FindMealAsync(Guid userId, Guid mealId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new meal.
    /// </summary>
    /// <param name="entry">Meal created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddMealAsync(MealEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to a meal previously returned by <see cref="FindMealAsync"/>.
    /// </summary>
    /// <param name="entry">Meal that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveMealAsync(MealEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes one meal owned by an account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="mealId">Meal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteMealAsync(Guid userId, Guid mealId, CancellationToken cancellationToken);
}
