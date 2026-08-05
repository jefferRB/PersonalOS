namespace PersonalOS.Domain.Nutrition;

/// <summary>
/// The daily calorie target, and optional macronutrient targets, chosen by one account.
/// </summary>
/// <remarks>
/// <para>
/// The account identifier is the primary key, so the database enforces one goal per account.
/// </para>
/// <para>
/// The target is a number the user chose for themselves. PersonalOS neither suggests it nor
/// judges it: the nutrition screens display consumed against target as plain arithmetic.
/// </para>
/// </remarks>
public sealed class NutritionGoal
{
    private NutritionGoal()
    {
    }

    /// <summary>Owning account, and the primary key.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Calories the user is aiming for each day.</summary>
    public int DailyCalorieTarget { get; private set; }

    /// <summary>Optional protein target, in grams.</summary>
    public decimal? ProteinTargetGrams { get; private set; }

    /// <summary>Optional carbohydrate target, in grams.</summary>
    public decimal? CarbohydrateTargetGrams { get; private set; }

    /// <summary>Optional fat target, in grams.</summary>
    public decimal? FatTargetGrams { get; private set; }

    /// <summary>Instant the goal was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the goal was last saved, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates the goal of one account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="dailyCalorieTarget">Calories the user is aiming for.</param>
    /// <param name="proteinTargetGrams">Optional protein target.</param>
    /// <param name="carbohydrateTargetGrams">Optional carbohydrate target.</param>
    /// <param name="fatTargetGrams">Optional fat target.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static NutritionGoal Create(
        Guid userId,
        int dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var goal = new NutritionGoal
        {
            UserId = userId,
            CreatedAtUtc = utcNow.ToUniversalTime(),
        };

        goal.Update(
            dailyCalorieTarget,
            proteinTargetGrams,
            carbohydrateTargetGrams,
            fatTargetGrams,
            utcNow);

        return goal;
    }

    /// <summary>
    /// Saves a new target.
    /// </summary>
    /// <param name="dailyCalorieTarget">Calories the user is aiming for.</param>
    /// <param name="proteinTargetGrams">Optional protein target.</param>
    /// <param name="carbohydrateTargetGrams">Optional carbohydrate target.</param>
    /// <param name="fatTargetGrams">Optional fat target.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(
        int dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams,
        DateTimeOffset utcNow)
    {
        if (!NutritionRules.IsCalorieTargetValid(dailyCalorieTarget))
        {
            throw new ArgumentOutOfRangeException(nameof(dailyCalorieTarget));
        }

        if (!NutritionRules.IsMacroValid(proteinTargetGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(proteinTargetGrams));
        }

        if (!NutritionRules.IsMacroValid(carbohydrateTargetGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(carbohydrateTargetGrams));
        }

        if (!NutritionRules.IsMacroValid(fatTargetGrams))
        {
            throw new ArgumentOutOfRangeException(nameof(fatTargetGrams));
        }

        DailyCalorieTarget = dailyCalorieTarget;
        ProteinTargetGrams = proteinTargetGrams;
        CarbohydrateTargetGrams = carbohydrateTargetGrams;
        FatTargetGrams = fatTargetGrams;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
