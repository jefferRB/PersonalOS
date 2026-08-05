using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Nutrition;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Nutrition;

/// <summary>
/// EF Core implementation of <see cref="INutritionStore"/>.
/// </summary>
public sealed class NutritionStore(ApplicationDbContext dbContext) : INutritionStore
{
    /// <inheritdoc />
    public async Task<NutritionGoal?> FindGoalAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.NutritionGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(goal => goal.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<NutritionGoal> SaveGoalAsync(
        Guid userId,
        int dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var goal = await dbContext.NutritionGoals
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (goal is null)
        {
            goal = NutritionGoal.Create(
                userId,
                dailyCalorieTarget,
                proteinTargetGrams,
                carbohydrateTargetGrams,
                fatTargetGrams,
                utcNow);

            dbContext.NutritionGoals.Add(goal);
        }
        else
        {
            goal.Update(
                dailyCalorieTarget,
                proteinTargetGrams,
                carbohydrateTargetGrams,
                fatTargetGrams,
                utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return goal;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MealEntry>> GetMealsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.MealEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId
                && entry.LocalDate >= from
                && entry.LocalDate <= to)
            .OrderBy(entry => entry.LocalDate)
            .ThenBy(entry => entry.OccurredAtLocalTime)
            // The name breaks ties between meals recorded without a time. Ordering by the creation
            // instant would read more naturally, but SQL Server and SQLite disagree about sorting
            // `datetimeoffset`, and the behaviour tests must not depend on which provider is in
            // use. A meaningful, portable tie-breaker is worth more than insertion order here.
            .ThenBy(entry => entry.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<MealEntry?> FindMealAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken) =>
        await dbContext.MealEntries
            .FirstOrDefaultAsync(
                entry => entry.Id == mealId && entry.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddMealAsync(MealEntry entry, CancellationToken cancellationToken)
    {
        dbContext.MealEntries.Add(entry);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveMealAsync(MealEntry entry, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteMealAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.MealEntries
            .Where(entry => entry.Id == mealId && entry.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
