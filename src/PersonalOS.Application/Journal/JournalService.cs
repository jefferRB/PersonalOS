using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Common;
using PersonalOS.Domain.Common;
using PersonalOS.Domain.Journal;

namespace PersonalOS.Application.Journal;

/// <summary>
/// Reads and saves the daily reflection of one account.
/// </summary>
/// <remarks>
/// <para>
/// Saving is an upsert keyed by account and local day, because the product rule is one entry per
/// day. A user who saves three times while writing ends with one entry, not three.
/// </para>
/// <para>
/// Nothing in this service, or in anything it calls, writes journal text to a log. The only
/// values ever logged for the journal are the account identifier, the date, and the outcome.
/// </para>
/// </remarks>
public sealed class JournalService(IJournalStore store, IClock clock)
{
    /// <summary>Contract field names used for section validation messages.</summary>
    public static readonly IReadOnlyList<string> SectionFields =
    [
        "wentWell",
        "wentPoorly",
        "cause",
        "lesson",
        "adjustmentForTomorrow",
        "freeNotes",
    ];

    /// <summary>
    /// Reads the entry of one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="localDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entry, or an empty one when the day has not been written about.</returns>
    public async Task<JournalEntryRecord> GetAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var entry = await store.FindAsync(userId, localDate, cancellationToken);

        return entry is null
            ? JournalEntryRecord.Empty(localDate)
            : JournalEntryRecord.FromEntity(entry);
    }

    /// <summary>
    /// Creates or updates the entry of one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="localDate">Local calendar day, taken from the route.</param>
    /// <param name="input">Submitted sections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<JournalEntryRecord>> SaveAsync(
        Guid userId,
        DateOnly localDate,
        JournalEntryInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = Validate(input);

        if (errors.HasErrors)
        {
            return OperationResult<JournalEntryRecord>.Invalid(errors.Build());
        }

        var utcNow = clock.UtcNow;
        var entry = await store.FindAsync(userId, localDate, cancellationToken);

        if (entry is null)
        {
            entry = DailyJournalEntry.Create(userId, localDate, utcNow);
            entry.Write(
                input.WentWell,
                input.WentPoorly,
                input.Cause,
                input.Lesson,
                input.AdjustmentForTomorrow,
                input.FreeNotes,
                utcNow);

            await store.AddAsync(entry, cancellationToken);
        }
        else
        {
            entry.Write(
                input.WentWell,
                input.WentPoorly,
                input.Cause,
                input.Lesson,
                input.AdjustmentForTomorrow,
                input.FreeNotes,
                utcNow);

            await store.SaveAsync(entry, cancellationToken);
        }

        return OperationResult<JournalEntryRecord>.Success(JournalEntryRecord.FromEntity(entry));
    }

    /// <summary>
    /// Reports which days inside an inclusive range already hold a written entry.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IReadOnlyList<DateOnly>> GetWrittenDatesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        store.GetWrittenDatesAsync(userId, from, to, cancellationToken);

    private static ValidationErrorCollector Validate(JournalEntryInput input)
    {
        var errors = new ValidationErrorCollector();
        var sections = new[]
        {
            input.WentWell,
            input.WentPoorly,
            input.Cause,
            input.Lesson,
            input.AdjustmentForTomorrow,
            input.FreeNotes,
        };

        for (var index = 0; index < sections.Length; index++)
        {
            if (!TextRules.TryNormalizeOptional(
                sections[index],
                DailyJournalEntry.SectionMaxLength,
                out _))
            {
                // The message states the limit but never repeats what the user wrote.
                errors.Add(
                    SectionFields[index],
                    $"This section must be {DailyJournalEntry.SectionMaxLength} characters or fewer.");
            }
        }

        return errors;
    }
}
