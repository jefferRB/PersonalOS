using PersonalOS.Application.Nutrition;
using PersonalOS.Domain.Nutrition;

namespace PersonalOS.Api.Contracts.Nutrition;

/// <summary>
/// The daily targets of the authenticated account.
/// </summary>
/// <param name="DailyCalorieTarget">Calories the user chose, or <see langword="null"/>.</param>
/// <param name="ProteinTargetGrams">Optional protein target.</param>
/// <param name="CarbohydrateTargetGrams">Optional carbohydrate target.</param>
/// <param name="FatTargetGrams">Optional fat target.</param>
/// <param name="UpdatedAtUtc">Instant the goal was last saved, in UTC.</param>
/// <remarks>
/// The target is a number the user chose. PersonalOS neither proposes one nor judges one.
/// </remarks>
public sealed record NutritionGoalResponse(
    int? DailyCalorieTarget,
    decimal? ProteinTargetGrams,
    decimal? CarbohydrateTargetGrams,
    decimal? FatTargetGrams,
    DateTimeOffset? UpdatedAtUtc)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static NutritionGoalResponse FromRecord(NutritionGoalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new NutritionGoalResponse(
            record.DailyCalorieTarget,
            record.ProteinTargetGrams,
            record.CarbohydrateTargetGrams,
            record.FatTargetGrams,
            record.UpdatedAtUtc);
    }
}

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
public sealed record MealEntryResponse(
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
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static MealEntryResponse FromRecord(MealEntryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new MealEntryResponse(
            record.Id,
            record.LocalDate,
            record.MealType,
            record.Name,
            record.Quantity,
            record.Calories,
            record.ProteinGrams,
            record.CarbohydrateGrams,
            record.FatGrams,
            record.OccurredAtLocalTime,
            record.Notes);
    }
}

/// <summary>
/// What one account ate on one local calendar day, beside the target they chose.
/// </summary>
/// <param name="LocalDate">The local calendar day.</param>
/// <param name="Goal">The targets the user chose.</param>
/// <param name="ConsumedCalories">Sum of the recorded calories.</param>
/// <param name="RemainingCalories">
/// Target minus consumed, or <see langword="null"/> without a target. The value goes negative once
/// the target is passed, which the interface states as a fact rather than as a warning.
/// </param>
/// <param name="ProteinGrams">Sum of the recorded protein.</param>
/// <param name="CarbohydrateGrams">Sum of the recorded carbohydrates.</param>
/// <param name="FatGrams">Sum of the recorded fat.</param>
/// <param name="Meals">The entries themselves, ordered by time.</param>
public sealed record NutritionDayResponse(
    DateOnly LocalDate,
    NutritionGoalResponse Goal,
    int ConsumedCalories,
    int? RemainingCalories,
    decimal ProteinGrams,
    decimal CarbohydrateGrams,
    decimal FatGrams,
    IReadOnlyList<MealEntryResponse> Meals)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static NutritionDayResponse FromRecord(NutritionDayRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new NutritionDayResponse(
            record.LocalDate,
            NutritionGoalResponse.FromRecord(record.Goal),
            record.ConsumedCalories,
            record.RemainingCalories,
            record.ProteinGrams,
            record.CarbohydrateGrams,
            record.FatGrams,
            [.. record.Meals.Select(MealEntryResponse.FromRecord)]);
    }
}

/// <summary>
/// Values a client may send when saving the daily targets.
/// </summary>
public sealed class SaveNutritionGoalRequest
{
    /// <summary>Calories the user is aiming for.</summary>
    public int? DailyCalorieTarget { get; init; }

    /// <summary>Optional protein target, in grams.</summary>
    public decimal? ProteinTargetGrams { get; init; }

    /// <summary>Optional carbohydrate target, in grams.</summary>
    public decimal? CarbohydrateTargetGrams { get; init; }

    /// <summary>Optional fat target, in grams.</summary>
    public decimal? FatTargetGrams { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public NutritionGoalInput ToInput() =>
        new(DailyCalorieTarget, ProteinTargetGrams, CarbohydrateTargetGrams, FatTargetGrams);
}

/// <summary>
/// Values a client may send when creating or editing a meal.
/// </summary>
public sealed class SaveMealRequest
{
    /// <summary>The owner's local calendar day, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? LocalDate { get; init; }

    /// <summary>Which meal of the day.</summary>
    public MealType MealType { get; init; } = MealType.Other;

    /// <summary>What was eaten.</summary>
    public string? Name { get; init; }

    /// <summary>Free-text amount, for example <c>200 g</c>.</summary>
    public string? Quantity { get; init; }

    /// <summary>Calories recorded.</summary>
    public int? Calories { get; init; }

    /// <summary>Optional protein, in grams.</summary>
    public decimal? ProteinGrams { get; init; }

    /// <summary>Optional carbohydrates, in grams.</summary>
    public decimal? CarbohydrateGrams { get; init; }

    /// <summary>Optional fat, in grams.</summary>
    public decimal? FatGrams { get; init; }

    /// <summary>Optional local time, as <c>HH:mm</c>.</summary>
    public TimeOnly? OccurredAtLocalTime { get; init; }

    /// <summary>Optional note.</summary>
    public string? Notes { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public MealEntryInput ToInput() =>
        new(
            LocalDate,
            MealType,
            Name,
            Quantity,
            Calories,
            ProteinGrams,
            CarbohydrateGrams,
            FatGrams,
            OccurredAtLocalTime,
            Notes);
}
