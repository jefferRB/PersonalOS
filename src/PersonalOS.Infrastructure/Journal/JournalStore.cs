using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Journal;
using PersonalOS.Domain.Journal;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Journal;

/// <summary>
/// EF Core implementation of <see cref="IJournalStore"/>.
/// </summary>
/// <remarks>
/// No method here logs, projects, or returns journal text except the one the journal screen calls
/// for a single day. <see cref="GetWrittenDatesAsync"/> in particular projects to dates in SQL, so
/// the reflection text never leaves the database to answer a summary question.
/// </remarks>
public sealed class JournalStore(ApplicationDbContext dbContext) : IJournalStore
{
    /// <inheritdoc />
    public async Task<DailyJournalEntry?> FindAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        await dbContext.DailyJournalEntries
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.LocalDate == localDate,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateOnly>> GetWrittenDatesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.DailyJournalEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId
                && entry.LocalDate >= from
                && entry.LocalDate <= to
                && (entry.WentWell != null
                    || entry.WentPoorly != null
                    || entry.Cause != null
                    || entry.Lesson != null
                    || entry.AdjustmentForTomorrow != null
                    || entry.FreeNotes != null))
            .Select(entry => entry.LocalDate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(DailyJournalEntry entry, CancellationToken cancellationToken)
    {
        dbContext.DailyJournalEntries.Add(entry);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(DailyJournalEntry entry, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
