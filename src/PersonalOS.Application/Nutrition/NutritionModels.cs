using PersonalOS.Domain.Nutrition;

namespace PersonalOS.Application.Nutrition;

/// <summary>
/// The daily targets of one account.
/// </summary>
/// <param name="DailyCalorieTarget">Calories the user is aiming for, or <see langword="null"/>.</param>
/// <param name="ProteinTargetGrams">Optional protein target.</param>
/// <param name="CarbohydrateTargetGrams">Optional carbohydrate target.</param>
/// <param name="FatTargetGrams">Optional fat target.</param>
/// <param name="UpdatedAtUtc">Instant the goal was last saved, in UTC.</param>
public sealed record NutritionGoalRecord(
    int? DailyCalorieTarget,
    decimal? ProteinTargetGrams,
    decimal? CarbohydrateTargetGrams,
    decimal? FatTargetGrams,
    DateTimeOffset? UpdatedAtUtc)
{
    /// <summary>An account that has not chosen a target yet.</summary>
    public static NutritionGoalRecord NotConfigured { get; } =
        new(null, null, null, null, null);

    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="goal">Domain entity.</param>
    public static NutritionGoalRecord FromEntity(NutritionGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);

        return new NutritionGoalRecord(
            goal.DailyCalorieTarget,
            goal.ProteinTargetGrams,
            goal.CarbohydrateTargetGrams,
            goal.FatTargetGrams,
            goal.UpdatedAtUtc);
    }
}

/// <summary>
/// Values a client may supply when saving the daily targets.
/// </summary>
/// <param name="DailyCalorieTarget">Calories the user is aiming for.</param>
/// <param name="ProteinTargetGrams">Optional protein target.</param>
/// <param name="CarbohydrateTargetGrams">Optional carbohydrate target.</param>
/// <param name="FatTargetGrams">Optional fat target.</param>
public sealed record NutritionGoalInput(
    int? DailyCalorieTarget,
    decimal? ProteinTargetGrams,
    decimal? CarbohydrateTargetGrams,
    decimal? FatTargetGrams);

/// <summary>
/// One recorded meal.
/// </summary>
/// <param name="Id">Entry identifier.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="MealType">Which meal of the day.</param>
/// <param name="Name">What was eaten.</param>
/// <param name="Quantity">Free-text amount.</param>
/// <param name="Calories">Calories the user recorded.</param>
/// <param name="ProteinGrams">Optional protein.</param>
/// <param name="CarbohydrateGrams">Optional carbohydrates.</param>
/// <param name="FatGrams">Optional fat.</param>
/// <param name="OccurredAtLocalTime">Optional local time.</param>
/// <param name="Notes">Optional note.</param>
public sealed record MealEntryRecord(
    Guid Id,
    DateOnly LocalDate,
    MealType MealType,
    string Name,
    string? Quantity,
    int Calories,
    decimal? ProteinGrams,
    decimal? CarbohydrateGrams,
    decimal? FatGrams,
    TimeOnly? OccurredAtLocalTime,
    string? Notes)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="entry">Domain entity.</param>
    public static MealEntryRecord FromEntity(MealEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new MealEntryRecord(
            entry.Id,
            entry.LocalDate,
            entry.MealType,
            entry.Name,
            entry.Quantity,
            entry.Calories,
            entry.ProteinGrams,
            entry.CarbohydrateGrams,
            entry.FatGrams,
            entry.OccurredAtLocalTime,
            entry.Notes);
    }
}

/// <summary>
/// Values a client may supply when creating or editing a meal.
/// </summary>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="MealType">Which meal of the day.</param>
/// <param name="Name">What was eaten.</param>
/// <param name="Quantity">Free-text amount.</param>
/// <param name="Calories">Calories recorded.</param>
/// <param name="ProteinGrams">Optional protein.</param>
/// <param name="CarbohydrateGrams">Optional carbohydrates.</param>
/// <param name="FatGrams">Optional fat.</param>
/// <param name="OccurredAtLocalTime">Optional local time.</param>
/// <param name="Notes">Optional note.</param>
public sealed record MealEntryInput(
    DateOnly? LocalDate,
    MealType MealType,
    string? Name,
    string? Quantity,
    int? Calories,
    decimal? ProteinGrams,
    decimal? CarbohydrateGrams,
    decimal? FatGrams,
    TimeOnly? OccurredAtLocalTime,
    string? Notes);

/// <summary>
/// What one account ate on one local calendar day, beside the target they chose.
/// </summary>
/// <param name="LocalDate">The local calendar day.</param>
/// <param name="Goal">The targets the user chose.</param>
/// <param name="ConsumedCalories">Sum of the recorded calories.</param>
/// <param name="ProteinGrams">Sum of the recorded protein.</param>
/// <param name="CarbohydrateGrams">Sum of the recorded carbohydrates.</param>
/// <param name="FatGrams">Sum of the recorded fat.</param>
/// <param name="Meals">The entries themselves, ordered by time.</param>
/// <remarks>
/// The record reports arithmetic and nothing else. There is no judgement, no rating, and no
/// advice: how many calories somebody should eat is not a question this application answers.
/// </remarks>
public sealed record NutritionDayRecord(
    DateOnly LocalDate,
    NutritionGoalRecord Goal,
    int ConsumedCalories,
    decimal ProteinGrams,
    decimal CarbohydrateGrams,
    decimal FatGrams,
    IReadOnlyList<MealEntryRecord> Meals)
{
    /// <summary>
    /// Calories left before the target is reached, or <see langword="null"/> without a target.
    /// </summary>
    /// <remarks>
    /// The value goes negative once the target is passed. That is a fact, and the interface states
    /// it plainly rather than hiding it or dressing it as a failure.
    /// </remarks>
    public int? RemainingCalories => Goal.DailyCalorieTarget is null
        ? null
        : Goal.DailyCalorieTarget.Value - ConsumedCalories;

    /// <summary>
    /// Builds the day summary from the meals recorded for it.
    /// </summary>
    /// <param name="localDate">The local calendar day.</param>
    /// <param name="goal">The targets the user chose.</param>
    /// <param name="meals">Meals recorded on that day.</param>
    public static NutritionDayRecord FromMeals(
        DateOnly localDate,
        NutritionGoalRecord goal,
        IReadOnlyList<MealEntry> meals)
    {
        ArgumentNullException.ThrowIfNull(meals);

        return new NutritionDayRecord(
            localDate,
            goal,
            meals.Sum(meal => meal.Calories),
            meals.Sum(meal => meal.ProteinGrams ?? 0m),
            meals.Sum(meal => meal.CarbohydrateGrams ?? 0m),
            meals.Sum(meal => meal.FatGrams ?? 0m),
            [.. meals.Select(MealEntryRecord.FromEntity)]);
    }
}
