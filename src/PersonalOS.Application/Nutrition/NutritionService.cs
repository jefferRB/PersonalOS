using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Common;
using PersonalOS.Domain.Common;
using PersonalOS.Domain.Nutrition;

namespace PersonalOS.Application.Nutrition;

/// <summary>
/// Records what one account ate and compares it with the target that account chose.
/// </summary>
/// <remarks>
/// <para>
/// The service performs arithmetic only. It never proposes a target, never labels a value as
/// healthy or unhealthy, never warns about a deficit or a surplus, and never scores a food.
/// PersonalOS is not a medical product and must not behave like one.
/// </para>
/// <para>
/// The range checks below exist so that the database stores numbers instead of typing mistakes.
/// They are technical bounds, not recommendations.
/// </para>
/// </remarks>
public sealed class NutritionService(INutritionStore store, IClock clock)
{
    /// <summary>Contract field name used for calorie-target validation messages.</summary>
    public const string DailyCalorieTargetField = "dailyCalorieTarget";

    /// <summary>Contract field name used for meal-name validation messages.</summary>
    public const string NameField = "name";

    /// <summary>Contract field name used for calorie validation messages.</summary>
    public const string CaloriesField = "calories";

    /// <summary>Contract field name used for date validation messages.</summary>
    public const string LocalDateField = "localDate";

    /// <summary>Contract field name used for protein validation messages.</summary>
    public const string ProteinField = "proteinGrams";

    /// <summary>Contract field name used for carbohydrate validation messages.</summary>
    public const string CarbohydrateField = "carbohydrateGrams";

    /// <summary>Contract field name used for fat validation messages.</summary>
    public const string FatField = "fatGrams";

    /// <summary>Contract field name used for note validation messages.</summary>
    public const string NotesField = "notes";

    /// <summary>
    /// Reads the goal of the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<NutritionGoalRecord> GetGoalAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var goal = await store.FindGoalAsync(userId, cancellationToken);

        return goal is null ? NutritionGoalRecord.NotConfigured : NutritionGoalRecord.FromEntity(goal);
    }

    /// <summary>
    /// Reads one local day, with its meals and totals.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="localDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<NutritionDayRecord> GetDayAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var goal = await GetGoalAsync(userId, cancellationToken);
        var meals = await store.GetMealsAsync(userId, localDate, localDate, cancellationToken);

        return NutritionDayRecord.FromMeals(localDate, goal, meals);
    }

    /// <summary>
    /// Saves the daily targets of the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<NutritionGoalRecord>> SaveGoalAsync(
        Guid userId,
        NutritionGoalInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();

        if (input.DailyCalorieTarget is null)
        {
            errors.Add(DailyCalorieTargetField, "Enter the daily calorie target you want to use.");
        }
        else if (!NutritionRules.IsCalorieTargetValid(input.DailyCalorieTarget.Value))
        {
            errors.Add(
                DailyCalorieTargetField,
                $"Enter a whole number between {NutritionRules.MinCalorieTarget} and {NutritionRules.MaxCalorieTarget}.");
        }

        AddMacroErrors(errors, input.ProteinTargetGrams, ProteinField);
        AddMacroErrors(errors, input.CarbohydrateTargetGrams, CarbohydrateField);
        AddMacroErrors(errors, input.FatTargetGrams, FatField);

        if (errors.HasErrors)
        {
            return OperationResult<NutritionGoalRecord>.Invalid(errors.Build());
        }

        var goal = await store.SaveGoalAsync(
            userId,
            input.DailyCalorieTarget!.Value,
            input.ProteinTargetGrams,
            input.CarbohydrateTargetGrams,
            input.FatTargetGrams,
            clock.UtcNow,
            cancellationToken);

        return OperationResult<NutritionGoalRecord>.Success(NutritionGoalRecord.FromEntity(goal));
    }

    /// <summary>
    /// Records a meal owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<MealEntryRecord>> CreateMealAsync(
        Guid userId,
        MealEntryInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = ValidateMeal(input);

        if (errors.HasErrors)
        {
            return OperationResult<MealEntryRecord>.Invalid(errors.Build());
        }

        var entry = MealEntry.Create(
            userId,
            input.LocalDate!.Value,
            input.MealType,
            input.Name,
            input.Quantity,
            input.Calories!.Value,
            input.ProteinGrams,
            input.CarbohydrateGrams,
            input.FatGrams,
            input.OccurredAtLocalTime,
            input.Notes,
            clock.UtcNow);

        await store.AddMealAsync(entry, cancellationToken);

        return OperationResult<MealEntryRecord>.Success(MealEntryRecord.FromEntity(entry));
    }

    /// <summary>
    /// Edits a meal owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="mealId">Meal identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<MealEntryRecord>> UpdateMealAsync(
        Guid userId,
        Guid mealId,
        MealEntryInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = ValidateMeal(input);

        if (errors.HasErrors)
        {
            return OperationResult<MealEntryRecord>.Invalid(errors.Build());
        }

        var entry = await store.FindMealAsync(userId, mealId, cancellationToken);

        if (entry is null)
        {
            return OperationResult<MealEntryRecord>.NotFound();
        }

        entry.Update(
            input.LocalDate!.Value,
            input.MealType,
            input.Name,
            input.Quantity,
            input.Calories!.Value,
            input.ProteinGrams,
            input.CarbohydrateGrams,
            input.FatGrams,
            input.OccurredAtLocalTime,
            input.Notes,
            clock.UtcNow);

        await store.SaveMealAsync(entry, cancellationToken);

        return OperationResult<MealEntryRecord>.Success(MealEntryRecord.FromEntity(entry));
    }

    /// <summary>
    /// Deletes a meal owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="mealId">Meal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<bool> DeleteMealAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken) =>
        store.DeleteMealAsync(userId, mealId, cancellationToken);

    private static ValidationErrorCollector ValidateMeal(MealEntryInput input)
    {
        var errors = new ValidationErrorCollector();

        if (!TextRules.TryNormalizeRequired(input.Name, 1, MealEntry.NameMaxLength, out _))
        {
            errors.Add(
                NameField,
                $"Enter what you ate, using {MealEntry.NameMaxLength} characters or fewer.");
        }

        if (input.LocalDate is null)
        {
            errors.Add(LocalDateField, "Choose the day this meal belongs to.");
        }

        if (input.Calories is null)
        {
            errors.Add(CaloriesField, "Enter the calories for this meal.");
        }
        else if (!NutritionRules.IsMealCaloriesValid(input.Calories.Value))
        {
            errors.Add(
                CaloriesField,
                $"Enter a whole number between 0 and {NutritionRules.MaxMealCalories}.");
        }

        AddMacroErrors(errors, input.ProteinGrams, ProteinField);
        AddMacroErrors(errors, input.CarbohydrateGrams, CarbohydrateField);
        AddMacroErrors(errors, input.FatGrams, FatField);

        if (!TextRules.TryNormalizeOptional(input.Notes, MealEntry.NotesMaxLength, out _))
        {
            errors.Add(
                NotesField,
                $"The note must be {MealEntry.NotesMaxLength} characters or fewer.");
        }

        return errors;
    }

    private static void AddMacroErrors(
        ValidationErrorCollector errors,
        decimal? value,
        string field)
    {
        errors.AddIf(
            !NutritionRules.IsMacroValid(value),
            field,
            $"Enter a value between 0 and {NutritionRules.MaxMacroGrams} grams.");
    }
}
