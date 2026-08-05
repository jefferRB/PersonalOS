using PersonalOS.Domain.Journal;

namespace PersonalOS.Application.Journal;

/// <summary>
/// The reflection written for one local calendar day.
/// </summary>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="WentWell">What went well.</param>
/// <param name="WentPoorly">What went poorly.</param>
/// <param name="Cause">Why it happened.</param>
/// <param name="Lesson">What was learned.</param>
/// <param name="AdjustmentForTomorrow">What should change tomorrow.</param>
/// <param name="FreeNotes">Anything else.</param>
/// <param name="UpdatedAtUtc">Instant the entry was last saved, in UTC.</param>
/// <param name="HasContent">Whether the entry holds any text.</param>
/// <remarks>
/// This record carries the most sensitive text in the product. It must never be logged, never
/// placed in a URL, and never written to browser storage.
/// </remarks>
public sealed record JournalEntryRecord(
    DateOnly LocalDate,
    string? WentWell,
    string? WentPoorly,
    string? Cause,
    string? Lesson,
    string? AdjustmentForTomorrow,
    string? FreeNotes,
    DateTimeOffset? UpdatedAtUtc,
    bool HasContent)
{
    /// <summary>
    /// Produces the record for a day the user has not written about yet.
    /// </summary>
    /// <param name="localDate">The local calendar day.</param>
    /// <remarks>
    /// An empty entry is returned instead of a 404 so the journal screen can open any day
    /// directly, and so a missing entry never looks like an error.
    /// </remarks>
    public static JournalEntryRecord Empty(DateOnly localDate) =>
        new(localDate, null, null, null, null, null, null, null, false);

    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="entry">Domain entity.</param>
    public static JournalEntryRecord FromEntity(DailyJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new JournalEntryRecord(
            entry.LocalDate,
            entry.WentWell,
            entry.WentPoorly,
            entry.Cause,
            entry.Lesson,
            entry.AdjustmentForTomorrow,
            entry.FreeNotes,
            entry.UpdatedAtUtc,
            entry.HasContent);
    }
}

/// <summary>
/// Values a client may supply when saving a journal entry.
/// </summary>
/// <param name="WentWell">What went well.</param>
/// <param name="WentPoorly">What went poorly.</param>
/// <param name="Cause">Why it happened.</param>
/// <param name="Lesson">What was learned.</param>
/// <param name="AdjustmentForTomorrow">What should change tomorrow.</param>
/// <param name="FreeNotes">Anything else.</param>
/// <remarks>
/// The contract carries no local date and no account identifier. The day comes from the route and
/// the account comes from the authentication cookie, so neither can be chosen through the body.
/// </remarks>
public sealed record JournalEntryInput(
    string? WentWell,
    string? WentPoorly,
    string? Cause,
    string? Lesson,
    string? AdjustmentForTomorrow,
    string? FreeNotes);
