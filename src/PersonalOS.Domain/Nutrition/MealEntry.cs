using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Nutrition;

/// <summary>
/// One thing the user ate or drank on one local calendar day.
/// </summary>
/// <remarks>
/// The entry stores what the user typed. PersonalOS has no food database and never looks a food
/// up anywhere, so nothing here is derived from an external source and no request leaves the
/// server because of a meal.
/// </remarks>
public sealed class MealEntry
{
    /// <summary>Maximum stored length of the meal name.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximum stored length of the quantity text.</summary>
    public const int QuantityMaxLength = 100;

    /// <summary>Maximum stored length of the meal notes.</summary>
    public const int NotesMaxLength = 1000;

    private MealEntry()
    {
    }

    /// <summary>Identifier of this entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The owner's local calendar day this entry belongs to.</summary>
    public DateOnly LocalDate { get; private set; }

    /// <summary>Which meal of the day this belongs to.</summary>
    public MealType MealType { get; private set; }

    /// <summary>What was eaten, as the user described it.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// How much was eaten, as free text such as <c>200 g</c> or <c>1 bowl</c>.
    /// </summary>
    /// <remarks>
    /// Quantity is text rather than a number and a unit. A structured amount would only be useful
    /// with a food database that could convert it into calories, and there is none.
    /// </remarks>
    public string? Quantity { get; private set; }

    /// <summary>Calories the user recorded.</summary>
    public int Calories { get; private set; }

    /// <summary>Optional protein, in grams.</summary>
    public decimal? ProteinGrams { get; private set; }

    /// <summary>Optional carbohydrates, in grams.</summary>
    public decimal? CarbohydrateGrams { get; private set; }

    /// <summary>Optional fat, in grams.</summary>
    public decimal? FatGrams { get; private set; }

    /// <summary>Optional local time the meal happened.</summary>
    public TimeOnly? OccurredAtLocalTime { get; private set; }

    /// <summary>Optional note.</summary>
    public string? Notes { get; private set; }

    /// <summary>Instant the entry was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the entry was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a meal entry owned by one account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="mealType">Which meal of the day.</param>
    /// <param name="name">What was eaten.</param>
    /// <param name="quantity">Optional free-text amount.</param>
    /// <param name="calories">Calories recorded.</param>
    /// <param name="proteinGrams">Optional protein.</param>
    /// <param name="carbohydrateGrams">Optional carbohydrates.</param>
    /// <param name="fatGrams">Optional fat.</param>
    /// <param name="occurredAtLocalTime">Optional local time.</param>
    /// <param name="notes">Optional note.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static MealEntry Create(
        Guid userId,
        DateOnly localDate,
        MealType mealType,
        string? name,
        string? quantity,
        int calories,
        decimal? proteinGrams,
        decimal? carbohydrateGrams,
        decimal? fatGrams,
        TimeOnly? occurredAtLocalTime,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = utcNow.ToUniversalTime(),
        };

        entry.Update(
            localDate,
            mealType,
            name,
            quantity,
            calories,
            proteinGrams,
            carbohydrateGrams,
            fatGrams,
            occurredAtLocalTime,
            notes,
            utcNow);

        return entry;
    }

    /// <summary>
    /// Applies an edit.
    /// </summary>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="mealType">Which meal of the day.</param>
    /// <param name="name">What was eaten.</param>
    /// <param name="quantity">Optional free-text amount.</param>
    /// <param name="calories">Calories recorded.</param>
    /// <param name="proteinGrams">Optional protein.</param>
    /// <param name="carbohydrateGrams">Optional carbohydrates.</param>
    /// <param name="fatGrams">Optional fat.</param>
    /// <param name="occurredAtLocalTime">Optional local time.</param>
    /// <param name="notes">Optional note.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(
        DateOnly localDate,
        MealType mealType,
        string? name,
        string? quantity,
        int calories,
        decimal? proteinGrams,
        decimal? carbohydrateGrams,
        decimal? fatGrams,
        TimeOnly? occurredAtLocalTime,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (!NutritionRules.IsMealCaloriesValid(calories))
        {
            throw new ArgumentOutOfRangeException(nameof(calories));
        }

        if (!NutritionRules.IsMacroValid(proteinGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(proteinGrams));
        }

        if (!NutritionRules.IsMacroValid(carbohydrateGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(carbohydrateGrams));
        }

        if (!NutritionRules.IsMacroValid(fatGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(fatGrams));
        }

        LocalDate = localDate;
        MealType = mealType;
        Name = TextRules.NormalizeRequiredOrThrow(name, 1, NameMaxLength, nameof(name));
        Quantity = TextRules.NormalizeOptionalOrThrow(quantity, QuantityMaxLength, nameof(quantity));
        Calories = calories;
        ProteinGrams = proteinGrams;
        CarbohydrateGrams = carbohydrateGrams;
        FatGrams = fatGrams;
        OccurredAtLocalTime = occurredAtLocalTime;
        Notes = TextRules.NormalizeOptionalOrThrow(notes, NotesMaxLength, nameof(notes));
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
