using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Calendar;
using PersonalOS.Domain.Planning;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Calendar;

/// <summary>
/// EF Core implementation of <see cref="ICalendarStore"/>.
/// </summary>
/// <remarks>
/// Every query filters by the account identifier the API derived from the authenticated principal,
/// including the ones that already carry a primary key. Filtering on the identifier as well as the
/// key is what makes "read another account's item by guessing its identifier" impossible rather than
/// merely unlikely.
/// </remarks>
public sealed class CalendarStore(ApplicationDbContext dbContext) : ICalendarStore
{
    /// <inheritdoc />
    /// <remarks>
    /// The predicate keeps the work in the database: an item is a candidate only when its series
    /// starts on or before the end of the window and has not already ended before the start of it.
    /// Both columns are covered by the composite index, so the scan never leaves this account's
    /// rows. Whether a candidate really produces a day inside the window is then decided in memory,
    /// because a recurrence rule is not something SQL can evaluate.
    /// </remarks>
    public async Task<IReadOnlyList<PlanningItem>> GetItemsOverlappingAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItems
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.StartDate <= to
                && (item.Recurrence.EndDate == null || item.Recurrence.EndDate >= from))
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesInRangeAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItemOccurrenceStates
            .AsNoTracking()
            .Where(state => state.UserId == userId
                && state.OccurrenceDate >= from
                && state.OccurrenceDate <= to)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PlanningItem?> FindItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItems
            .FirstOrDefaultAsync(
                item => item.Id == itemId && item.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasOccurrenceStatesAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItemOccurrenceStates
            .AsNoTracking()
            .AnyAsync(
                state => state.PlanningItemId == itemId && state.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<PlanningItemOccurrenceState?> FindStateAsync(
        Guid userId,
        Guid itemId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItemOccurrenceStates
            .FirstOrDefaultAsync(
                state => state.PlanningItemId == itemId
                    && state.UserId == userId
                    && state.OccurrenceDate == occurrenceDate,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesForItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        await dbContext.PlanningItemOccurrenceStates
            .Where(state => state.PlanningItemId == itemId && state.UserId == userId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddItemAsync(PlanningItem item, CancellationToken cancellationToken)
    {
        dbContext.PlanningItems.Add(item);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddStateAsync(
        PlanningItemOccurrenceState state,
        CancellationToken cancellationToken)
    {
        dbContext.PlanningItemOccurrenceStates.Add(state);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The occurrence states go with the item through the cascade configured on the relationship,
    /// so deleting a series never leaves behind rows that describe days nothing produces any more.
    /// </remarks>
    public async Task<bool> DeleteItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.PlanningItems
            .Where(item => item.Id == itemId && item.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
