using PersonalOS.Domain.Journal;

namespace PersonalOS.Application.Journal;

/// <summary>
/// Persistence port for daily journal entries.
/// </summary>
/// <remarks>
/// Journal rows are the most sensitive data PersonalOS holds. Implementations must scope every
/// query by the account identifier the API derived from the authentication cookie, and must never
/// write journal text to a log.
/// </remarks>
public interface IJournalStore
{
    /// <summary>
    /// Finds the entry an account wrote for one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="localDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entry, or <see langword="null"/> when nothing was written that day.</returns>
    Task<DailyJournalEntry?> FindAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reports which days inside an inclusive range already hold an entry with text.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Only the dates are returned. Today needs to know whether a day was reflected on, and must
    /// never receive the reflection itself to answer that question.
    /// </remarks>
    Task<IReadOnlyList<DateOnly>> GetWrittenDatesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new entry.
    /// </summary>
    /// <param name="entry">Entry created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(DailyJournalEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to an entry previously returned by <see cref="FindAsync"/>.
    /// </summary>
    /// <param name="entry">Entry that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(DailyJournalEntry entry, CancellationToken cancellationToken);
}
