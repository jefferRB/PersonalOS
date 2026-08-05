namespace PersonalOS.Domain.Nutrition;

/// <summary>
/// Which meal of the day an entry belongs to.
/// </summary>
public enum MealType
{
    /// <summary>The first meal of the day.</summary>
    Breakfast = 0,

    /// <summary>The midday meal.</summary>
    Lunch = 1,

    /// <summary>The evening meal.</summary>
    Dinner = 2,

    /// <summary>Anything eaten between meals.</summary>
    Snack = 3,

    /// <summary>Anything that does not fit the groups above.</summary>
    Other = 4,
}

/// <summary>
/// Technical ranges accepted by the nutrition module.
/// </summary>
/// <remarks>
/// <para>
/// These bounds exist so the database stores numbers rather than typing mistakes. They are not
/// health advice, and PersonalOS deliberately makes no claim about which values are appropriate
/// for any person. Deciding what someone should eat is a decision for that person and, where it
/// matters, a qualified professional.
/// </para>
/// <para>
/// For the same reason the module never labels a value as healthy or unhealthy, never warns about
/// a deficit, and never scores a food.
/// </para>
/// </remarks>
public static class NutritionRules
{
    /// <summary>Smallest daily calorie target the module will store.</summary>
    public const int MinCalorieTarget = 500;

    /// <summary>Largest daily calorie target the module will store.</summary>
    public const int MaxCalorieTarget = 20000;

    /// <summary>Largest calorie value accepted for one meal entry.</summary>
    public const int MaxMealCalories = 20000;

    /// <summary>Largest macronutrient value accepted, in grams.</summary>
    public const decimal MaxMacroGrams = 2000m;

    /// <summary>
    /// Reports whether a daily calorie target is inside the stored range.
    /// </summary>
    /// <param name="value">Candidate target.</param>
    public static bool IsCalorieTargetValid(int value) =>
        value is >= MinCalorieTarget and <= MaxCalorieTarget;

    /// <summary>
    /// Reports whether a meal calorie value is inside the stored range.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <remarks>Zero is accepted: a drink with no calories is still worth recording.</remarks>
    public static bool IsMealCaloriesValid(int value) => value is >= 0 and <= MaxMealCalories;

    /// <summary>
    /// Reports whether an optional macronutrient value is inside the stored range.
    /// </summary>
    /// <param name="value">Candidate value in grams.</param>
    public static bool IsMacroValid(decimal? value) =>
        value is null or (>= 0m and <= MaxMacroGrams);
}
