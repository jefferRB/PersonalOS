using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Journal;

/// <summary>
/// The user's reflection for one local calendar day.
/// </summary>
/// <remarks>
/// <para>
/// This is the most sensitive record in PersonalOS. It can describe health, relationships, work
/// conflicts, and plans the user has told nobody else. The rules that follow from that are
/// enforced across every layer: the body is never written to a log, never placed in a URL, never
/// stored in the browser, never rendered as HTML, and every response carries
/// <c>Cache-Control: no-store</c>.
/// </para>
/// <para>
/// Exactly one entry may exist per account per local day, enforced by a unique index. Saving the
/// same day twice updates the existing entry rather than accumulating duplicates, which is what
/// makes the journal safe to save repeatedly while the user is still writing.
/// </para>
/// </remarks>
public sealed class DailyJournalEntry
{
    /// <summary>
    /// Maximum stored length of each reflection section.
    /// </summary>
    /// <remarks>
    /// The limit bounds one request and one row. It is generous enough for a long reflection and
    /// small enough that a single account cannot fill the database through this endpoint.
    /// </remarks>
    public const int SectionMaxLength = 4000;

    private DailyJournalEntry()
    {
    }

    /// <summary>Identifier of this entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The owner's local calendar day this entry describes.</summary>
    public DateOnly LocalDate { get; private set; }

    /// <summary>What went well.</summary>
    public string? WentWell { get; private set; }

    /// <summary>What went poorly.</summary>
    public string? WentPoorly { get; private set; }

    /// <summary>Why it happened.</summary>
    public string? Cause { get; private set; }

    /// <summary>What was learned.</summary>
    public string? Lesson { get; private set; }

    /// <summary>What should change tomorrow.</summary>
    public string? AdjustmentForTomorrow { get; private set; }

    /// <summary>Anything else worth remembering.</summary>
    public string? FreeNotes { get; private set; }

    /// <summary>Instant the entry was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the entry was last saved, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Whether the entry holds any text at all.
    /// </summary>
    /// <remarks>
    /// Today reports only this boolean. Whether a day was reflected on is not private in the way
    /// the reflection itself is, and the summary must never carry the text.
    /// </remarks>
    public bool HasContent =>
        WentWell is not null
        || WentPoorly is not null
        || Cause is not null
        || Lesson is not null
        || AdjustmentForTomorrow is not null
        || FreeNotes is not null;

    /// <summary>
    /// Creates the entry for one local day.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static DailyJournalEntry Create(
        Guid userId,
        DateOnly localDate,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var createdAt = utcNow.ToUniversalTime();

        return new DailyJournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LocalDate = localDate,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
        };
    }

    /// <summary>
    /// Saves the reflection sections.
    /// </summary>
    /// <param name="wentWell">What went well.</param>
    /// <param name="wentPoorly">What went poorly.</param>
    /// <param name="cause">Why it happened.</param>
    /// <param name="lesson">What was learned.</param>
    /// <param name="adjustmentForTomorrow">What should change tomorrow.</param>
    /// <param name="freeNotes">Anything else.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// Every section is optional. A day where the user only wanted to write one sentence is a
    /// complete entry, and the form must not force six answers to accept one.
    /// </remarks>
    public void Write(
        string? wentWell,
        string? wentPoorly,
        string? cause,
        string? lesson,
        string? adjustmentForTomorrow,
        string? freeNotes,
        DateTimeOffset utcNow)
    {
        WentWell = Normalize(wentWell, nameof(wentWell));
        WentPoorly = Normalize(wentPoorly, nameof(wentPoorly));
        Cause = Normalize(cause, nameof(cause));
        Lesson = Normalize(lesson, nameof(lesson));
        AdjustmentForTomorrow = Normalize(adjustmentForTomorrow, nameof(adjustmentForTomorrow));
        FreeNotes = Normalize(freeNotes, nameof(freeNotes));
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    private static string? Normalize(string? value, string parameterName) =>
        TextRules.NormalizeOptionalOrThrow(value, SectionMaxLength, parameterName);
}
