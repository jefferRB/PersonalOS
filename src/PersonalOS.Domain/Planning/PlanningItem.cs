using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Planning;

/// <summary>
/// One thing the user placed on the calendar, together with the rule for when it happens.
/// </summary>
/// <remarks>
/// <para>
/// A planning item is the single calendar aggregate. A task, a routine, an event, and an
/// appointment differ only by <see cref="Kind"/>: they carry the same fields and obey the same
/// rules, so four classes would express four different words on a chip and nothing else.
/// </para>
/// <para>
/// The item owns its <see cref="Recurrence"/>. A repeating item is therefore one row, whatever its
/// horizon, and the days it applies to are calculated for the window a screen is showing. The only
/// rows a repetition ever writes are <see cref="PlanningItemOccurrenceState"/>, and only once the
/// user acts on a specific day.
/// </para>
/// <para>
/// <see cref="StartDate"/> is a <see cref="DateOnly"/> in the owner's time zone, not an instant. A
/// meeting "at 09:00 on Monday" stays at 09:00 on Monday even if the account later moves to another
/// zone, which is the behaviour a personal planner needs. Instants that record when something
/// really happened are stored in UTC.
/// </para>
/// </remarks>
public sealed class PlanningItem
{
    /// <summary>Maximum stored length of the title.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Minimum length of the title after trimming.</summary>
    public const int TitleMinLength = 1;

    /// <summary>Maximum stored length of the description.</summary>
    public const int DescriptionMaxLength = 2000;

    private PlanningItem()
    {
    }

    /// <summary>Identifier of this item.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Short description shown on the calendar and the agenda.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Optional longer text. Private content: never logged and never put in a summary.</summary>
    public string? Description { get; private set; }

    /// <summary>What sort of thing this is.</summary>
    public PlanningItemKind Kind { get; private set; }

    /// <summary>Which area of life this belongs to.</summary>
    public PlanningCategory Category { get; private set; }

    /// <summary>How much this matters.</summary>
    public PlanningPriority Priority { get; private set; }

    /// <summary>The owner's local calendar day the series starts on.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Local start time, or <see langword="null"/> for an item with no time.</summary>
    public TimeOnly? StartTime { get; private set; }

    /// <summary>Local end time, or <see langword="null"/>.</summary>
    public TimeOnly? EndTime { get; private set; }

    /// <summary>Which local calendar days this item applies to.</summary>
    public PlanningRecurrence Recurrence { get; private set; } = null!;

    /// <summary>Instant the item was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the item was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Reports whether a start and end time pair is acceptable.
    /// </summary>
    /// <param name="startTime">Local start time.</param>
    /// <param name="endTime">Local end time.</param>
    /// <returns>
    /// <see langword="false"/> when an end time was given without a start time, or when the end is
    /// not strictly after the start.
    /// </returns>
    /// <remarks>
    /// An item that ends exactly when it starts occupies no time and is almost always a typing
    /// mistake, so the comparison is strict. Items that cross midnight are not supported: the second
    /// half belongs to the next calendar day.
    /// </remarks>
    public static bool IsTimeRangeValid(TimeOnly? startTime, TimeOnly? endTime)
    {
        if (endTime is null)
        {
            return true;
        }

        return startTime is not null && endTime.Value > startTime.Value;
    }

    /// <summary>
    /// Creates a calendar item owned by one account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="title">Title. Trimmed before storing.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="kind">What sort of thing this is.</param>
    /// <param name="category">Which area of life this belongs to.</param>
    /// <param name="priority">How much this matters.</param>
    /// <param name="startDate">The owner's local calendar day the series starts on.</param>
    /// <param name="startTime">Optional local start time.</param>
    /// <param name="endTime">Optional local end time.</param>
    /// <param name="recurrence">Validated recurrence rule.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static PlanningItem Create(
        Guid userId,
        string? title,
        string? description,
        PlanningItemKind kind,
        PlanningCategory category,
        PlanningPriority priority,
        DateOnly startDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        PlanningRecurrence recurrence,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(recurrence);

        if (!IsTimeRangeValid(startTime, endTime))
        {
            throw new ArgumentException(
                "The end time must be after the start time.",
                nameof(endTime));
        }

        if (!PlanningRecurrence.IsEndDateValid(startDate, recurrence.EndDate))
        {
            throw new ArgumentException(
                "The repeat end date cannot be before the start date.",
                nameof(recurrence));
        }

        var createdAt = utcNow.ToUniversalTime();

        return new PlanningItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = TextRules.NormalizeRequiredOrThrow(
                title,
                TitleMinLength,
                TitleMaxLength,
                nameof(title)),
            Description = TextRules.NormalizeOptionalOrThrow(
                description,
                DescriptionMaxLength,
                nameof(description)),
            Kind = kind,
            Category = category,
            Priority = priority,
            StartDate = startDate,
            StartTime = startTime,
            EndTime = endTime,
            Recurrence = recurrence,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
        };
    }

    /// <summary>
    /// Reports whether an edit may be applied to this item.
    /// </summary>
    /// <param name="startDate">Proposed start date.</param>
    /// <param name="recurrence">Proposed recurrence rule.</param>
    /// <param name="hasOccurrenceStates">
    /// Whether the user has already completed, reopened, or cancelled at least one occurrence.
    /// </param>
    /// <returns>Why the edit is refused, or <see cref="PlanningEditRefusal.None"/>.</returns>
    /// <remarks>
    /// <para>
    /// Once a day has been acted on, the days a rule produces are no longer the model's business
    /// alone: they are days the user has decided things about. Moving them would silently reattach
    /// a completion to a date the user never looked at, so the pattern is frozen and only the end
    /// date may be set or brought forward.
    /// </para>
    /// <para>
    /// A one-off item is exempt. It has no pattern to protect, and rescheduling a task that was
    /// already completed once is an ordinary thing to want; the application layer moves its single
    /// occurrence state to the new date.
    /// </para>
    /// </remarks>
    public PlanningEditRefusal CanApplyEdit(
        DateOnly startDate,
        PlanningRecurrence recurrence,
        bool hasOccurrenceStates)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        if (!PlanningRecurrence.IsEndDateValid(startDate, recurrence.EndDate))
        {
            return PlanningEditRefusal.EndDateBeforeStartDate;
        }

        if (!hasOccurrenceStates)
        {
            return PlanningEditRefusal.None;
        }

        // A one-off item has no series to protect.
        if (!Recurrence.Repeats && !recurrence.Repeats)
        {
            return PlanningEditRefusal.None;
        }

        if (!Recurrence.HasSamePattern(recurrence))
        {
            return PlanningEditRefusal.PatternLocked;
        }

        if (startDate != StartDate)
        {
            return PlanningEditRefusal.StartDateLocked;
        }

        return Recurrence.AllowsEndDateChangeTo(recurrence.EndDate)
            ? PlanningEditRefusal.None
            : PlanningEditRefusal.EndDateMayOnlyBeShortened;
    }

    /// <summary>
    /// Applies an edit to the whole series.
    /// </summary>
    /// <param name="title">Title. Trimmed before storing.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="kind">What sort of thing this is.</param>
    /// <param name="category">Which area of life this belongs to.</param>
    /// <param name="priority">How much this matters.</param>
    /// <param name="startDate">The owner's local calendar day the series starts on.</param>
    /// <param name="startTime">Optional local start time.</param>
    /// <param name="endTime">Optional local end time.</param>
    /// <param name="recurrence">Validated recurrence rule.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// Content and times are shared by every occurrence, so an edit changes the whole series. Editing
    /// a single occurrence's content is not supported in this version: it would need a second copy
    /// of every field per day, and a rule for what happens to those copies when the series changes.
    /// </remarks>
    public void Update(
        string? title,
        string? description,
        PlanningItemKind kind,
        PlanningCategory category,
        PlanningPriority priority,
        DateOnly startDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        PlanningRecurrence recurrence,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        if (!IsTimeRangeValid(startTime, endTime))
        {
            throw new ArgumentException(
                "The end time must be after the start time.",
                nameof(endTime));
        }

        if (!PlanningRecurrence.IsEndDateValid(startDate, recurrence.EndDate))
        {
            throw new ArgumentException(
                "The repeat end date cannot be before the start date.",
                nameof(recurrence));
        }

        Title = TextRules.NormalizeRequiredOrThrow(
            title,
            TitleMinLength,
            TitleMaxLength,
            nameof(title));
        Description = TextRules.NormalizeOptionalOrThrow(
            description,
            DescriptionMaxLength,
            nameof(description));
        Kind = kind;
        Category = category;
        Priority = priority;
        StartDate = startDate;
        StartTime = startTime;
        EndTime = endTime;
        Recurrence = recurrence;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Reports whether this item applies to one local calendar day.
    /// </summary>
    /// <param name="date">Local calendar day being tested.</param>
    public bool OccursOn(DateOnly date) => Recurrence.OccursOn(StartDate, date);

    /// <summary>
    /// Lists the local calendar days this item applies to inside an inclusive range.
    /// </summary>
    /// <param name="from">First day to test.</param>
    /// <param name="to">Last day to test.</param>
    public IEnumerable<DateOnly> OccurrencesBetween(DateOnly from, DateOnly to) =>
        Recurrence.OccurrencesBetween(StartDate, from, to);
}

/// <summary>
/// Why an edit to a calendar item cannot be applied.
/// </summary>
public enum PlanningEditRefusal
{
    /// <summary>The edit is acceptable.</summary>
    None = 0,

    /// <summary>The repeat end date is before the start date.</summary>
    EndDateBeforeStartDate = 1,

    /// <summary>The repetition itself cannot change once occurrences have been acted on.</summary>
    PatternLocked = 2,

    /// <summary>The start date cannot move once occurrences have been acted on.</summary>
    StartDateLocked = 3,

    /// <summary>An established series may only be ended or shortened, never extended.</summary>
    EndDateMayOnlyBeShortened = 4,
}
