using PersonalOS.Application.Journal;

namespace PersonalOS.Api.Contracts.Journal;

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
/// Journal responses always carry <c>Cache-Control: no-store</c>, are never logged, and are never
/// written to browser storage by the Angular client.
/// </remarks>
public sealed record JournalEntryResponse(
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
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static JournalEntryResponse FromRecord(JournalEntryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new JournalEntryResponse(
            record.LocalDate,
            record.WentWell,
            record.WentPoorly,
            record.Cause,
            record.Lesson,
            record.AdjustmentForTomorrow,
            record.FreeNotes,
            record.UpdatedAtUtc,
            record.HasContent);
    }
}

/// <summary>
/// Values a client may send when saving a journal entry.
/// </summary>
/// <remarks>
/// The contract carries no date and no account identifier. The day comes from the route and the
/// account from the authentication cookie, so neither can be chosen through the body.
/// </remarks>
public sealed class SaveJournalEntryRequest
{
    /// <summary>What went well.</summary>
    public string? WentWell { get; init; }

    /// <summary>What went poorly.</summary>
    public string? WentPoorly { get; init; }

    /// <summary>Why it happened.</summary>
    public string? Cause { get; init; }

    /// <summary>What was learned.</summary>
    public string? Lesson { get; init; }

    /// <summary>What should change tomorrow.</summary>
    public string? AdjustmentForTomorrow { get; init; }

    /// <summary>Anything else.</summary>
    public string? FreeNotes { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public JournalEntryInput ToInput() =>
        new(WentWell, WentPoorly, Cause, Lesson, AdjustmentForTomorrow, FreeNotes);
}
