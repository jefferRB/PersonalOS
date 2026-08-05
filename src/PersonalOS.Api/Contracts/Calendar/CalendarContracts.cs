using PersonalOS.Application.Calendar;
using PersonalOS.Domain.Planning;

namespace PersonalOS.Api.Contracts.Calendar;

/// <summary>
/// A recurrence rule, as the calendar endpoints return it.
/// </summary>
/// <param name="Frequency">How often the item repeats.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="EndDate">Optional last local calendar day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="SelectedWeekdays">Chosen weekdays, from Sunday to Saturday.</param>
public sealed record RecurrenceResponse(
    PlanningRecurrenceFrequency Frequency,
    int Interval,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek> SelectedWeekdays)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RecurrenceResponse FromRecord(PlanningRecurrenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RecurrenceResponse(
            record.Frequency,
            record.Interval,
            record.EndDate,
            record.SelectedWeekdays);
    }
}

/// <summary>
/// One calendar item with its rule, as the editor loads it.
/// </summary>
/// <param name="Id">Item identifier.</param>
/// <param name="Title">Short description.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Kind">What sort of thing this is.</param>
/// <param name="Category">Which area of life this belongs to.</param>
/// <param name="Priority">How much this matters.</param>
/// <param name="StartDate">First local calendar day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="StartTime">Optional local start time, as <c>HH:mm</c>.</param>
/// <param name="EndTime">Optional local end time, as <c>HH:mm</c>.</param>
/// <param name="Recurrence">Which local calendar days this item applies to.</param>
/// <param name="IsRecurrencePatternLocked">
/// Whether the repetition can still be changed, so the editor can disable those controls rather
/// than letting the server refuse the save.
/// </param>
/// <remarks>
/// The contract carries no account identifier. The caller already proved who it is with the
/// authentication cookie, and an identifier on the wire would only invite a client to send one back
/// as if it granted access.
/// </remarks>
public sealed record PlanningItemResponse(
    Guid Id,
    string Title,
    string? Description,
    PlanningItemKind Kind,
    PlanningCategory Category,
    PlanningPriority Priority,
    DateOnly StartDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    RecurrenceResponse Recurrence,
    bool IsRecurrencePatternLocked)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static PlanningItemResponse FromRecord(PlanningItemRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new PlanningItemResponse(
            record.Id,
            record.Title,
            record.Description,
            record.Kind,
            record.Category,
            record.Priority,
            record.StartDate,
            record.StartTime,
            record.EndTime,
            RecurrenceResponse.FromRecord(record.Recurrence),
            record.IsRecurrencePatternLocked);
    }
}

/// <summary>
/// One calendar item on one local calendar day.
/// </summary>
/// <param name="PlanningItemId">Item this occurrence belongs to.</param>
/// <param name="OccurrenceDate">The local calendar day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="Title">Short description.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Kind">What sort of thing this is.</param>
/// <param name="Category">Which area of life this belongs to.</param>
/// <param name="Priority">How much this matters.</param>
/// <param name="StartTime">Optional local start time, as <c>HH:mm</c>.</param>
/// <param name="EndTime">Optional local end time, as <c>HH:mm</c>.</param>
/// <param name="Status">What the user decided about this day.</param>
/// <param name="IsRecurring">Whether the item repeats.</param>
/// <param name="IsImportant">
/// Whether the activity is one the user should not be surprised by. Events and appointments always
/// are; a task or a routine only when marked high priority.
/// </param>
/// <param name="CompletedAtUtc">Instant this occurrence was completed, in UTC.</param>
public sealed record CalendarOccurrenceResponse(
    Guid PlanningItemId,
    DateOnly OccurrenceDate,
    string Title,
    string? Description,
    PlanningItemKind Kind,
    PlanningCategory Category,
    PlanningPriority Priority,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    OccurrenceStatus Status,
    bool IsRecurring,
    bool IsImportant,
    DateTimeOffset? CompletedAtUtc)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static CalendarOccurrenceResponse FromRecord(CalendarOccurrenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CalendarOccurrenceResponse(
            record.PlanningItemId,
            record.OccurrenceDate,
            record.Title,
            record.Description,
            record.Kind,
            record.Category,
            record.Priority,
            record.StartTime,
            record.EndTime,
            record.Status,
            record.IsRecurring,
            record.IsImportant,
            record.CompletedAtUtc);
    }
}

/// <summary>
/// What one cell of the month grid needs to render.
/// </summary>
/// <param name="Date">The local calendar day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="TotalCount">How many occurrences fall on the day.</param>
/// <param name="CompletedCount">How many of them are finished.</param>
/// <param name="FailedCount">How many were expected and did not happen.</param>
/// <param name="CancelledCount">How many were called off.</param>
/// <param name="Kinds">Which kinds appear on the day and how many of each, busiest first.</param>
/// <param name="HasHighPriority">Whether anything on the day is marked important.</param>
/// <remarks>
/// The summary carries no title and no description on purpose. A month view would otherwise ship a
/// grid's worth of private text that nothing on screen displays.
/// </remarks>
public sealed record CalendarDaySummaryResponse(
    DateOnly Date,
    int TotalCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount,
    IReadOnlyList<DayKindCountResponse> Kinds,
    bool HasHighPriority)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static CalendarDaySummaryResponse FromRecord(CalendarDaySummaryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CalendarDaySummaryResponse(
            record.Date,
            record.TotalCount,
            record.CompletedCount,
            record.FailedCount,
            record.CancelledCount,
            [.. record.Kinds.Select(DayKindCountResponse.FromRecord)],
            record.HasHighPriority);
    }
}

/// <summary>
/// How many of one kind fall on a day.
/// </summary>
/// <param name="Kind">What sort of thing these are.</param>
/// <param name="Count">How many of them the day holds.</param>
public sealed record DayKindCountResponse(PlanningItemKind Kind, int Count)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static DayKindCountResponse FromRecord(DayKindCountRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new DayKindCountResponse(record.Kind, record.Count);
    }
}

/// <summary>
/// One month of the calendar grid.
/// </summary>
/// <param name="Year">Year being shown.</param>
/// <param name="Month">Month being shown, from 1 to 12.</param>
/// <param name="FromDate">First day covered by the grid.</param>
/// <param name="ToDate">Last day covered by the grid.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="Days">Summaries for the days that hold anything, in date order.</param>
public sealed record CalendarMonthResponse(
    int Year,
    int Month,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<CalendarDaySummaryResponse> Days)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static CalendarMonthResponse FromRecord(CalendarMonthRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CalendarMonthResponse(
            record.Year,
            record.Month,
            record.FromDate,
            record.ToDate,
            record.TodayLocalDate,
            record.TimeZoneId,
            [.. record.Days.Select(CalendarDaySummaryResponse.FromRecord)]);
    }
}

/// <summary>
/// Everything the daily agenda and the day planner show for one local calendar day.
/// </summary>
/// <param name="Date">The local calendar day being shown, as <c>yyyy-MM-dd</c>.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="LocalTimeOfDay">The account's current local time, as <c>HH:mm:ss</c>.</param>
/// <param name="Occurrences">Occurrences on the day, untimed first, then in time order.</param>
public sealed record CalendarDayResponse(
    DateOnly Date,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceResponse> Occurrences)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static CalendarDayResponse FromRecord(CalendarDayRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CalendarDayResponse(
            record.Date,
            record.TodayLocalDate,
            record.TimeZoneId,
            record.LocalTimeOfDay,
            [.. record.Occurrences.Select(CalendarOccurrenceResponse.FromRecord)]);
    }
}

/// <summary>
/// The occurrences of one local calendar day inside the upcoming window.
/// </summary>
/// <param name="Date">The local calendar day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="Occurrences">Everything on that day, untimed first, then in time order.</param>
public sealed record UpcomingDayResponse(
    DateOnly Date,
    IReadOnlyList<CalendarOccurrenceResponse> Occurrences)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static UpcomingDayResponse FromRecord(UpcomingDayRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new UpcomingDayResponse(
            record.Date,
            [.. record.Occurrences.Select(CalendarOccurrenceResponse.FromRecord)]);
    }
}

/// <summary>
/// The next seven local days.
/// </summary>
/// <param name="FromDate">First day covered, as <c>yyyy-MM-dd</c>.</param>
/// <param name="ToDate">Last day covered, as <c>yyyy-MM-dd</c>.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="Days">Days that hold something, in date order. Empty days are omitted.</param>
public sealed record UpcomingWeekResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<UpcomingDayResponse> Days)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static UpcomingWeekResponse FromRecord(UpcomingWeekRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new UpcomingWeekResponse(
            record.FromDate,
            record.ToDate,
            record.TodayLocalDate,
            record.TimeZoneId,
            [.. record.Days.Select(UpcomingDayResponse.FromRecord)]);
    }
}

/// <summary>
/// Values a client may send for a recurrence rule.
/// </summary>
public sealed class SaveRecurrenceRequest
{
    /// <summary>How often the item repeats. Defaults to not repeating.</summary>
    public PlanningRecurrenceFrequency Frequency { get; init; } = PlanningRecurrenceFrequency.None;

    /// <summary>Distance between occurrences.</summary>
    public int Interval { get; init; } = 1;

    /// <summary>Optional last local calendar day, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Chosen weekdays for a weekly rule.</summary>
    public IReadOnlyList<DayOfWeek>? SelectedWeekdays { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public PlanningRecurrenceInput ToInput() =>
        new(Frequency, Interval, EndDate, SelectedWeekdays);
}

/// <summary>
/// Values a client may send when creating or editing a calendar item.
/// </summary>
/// <remarks>
/// The contract exposes only editable fields. It carries no account identifier, no occurrence
/// status, and no timestamps, so a client cannot claim another account's item, fake a completion
/// instant, or set a status without going through the dedicated endpoint. Unknown JSON properties
/// are ignored.
/// </remarks>
public sealed class SavePlanningItemRequest
{
    /// <summary>Short description. The server trims it.</summary>
    public string? Title { get; init; }

    /// <summary>Optional longer text.</summary>
    public string? Description { get; init; }

    /// <summary>What sort of thing this is. Defaults to <see cref="PlanningItemKind.Task"/>.</summary>
    public PlanningItemKind Kind { get; init; } = PlanningItemKind.Task;

    /// <summary>Which area of life this belongs to.</summary>
    public PlanningCategory Category { get; init; } = PlanningCategory.General;

    /// <summary>How much this matters.</summary>
    public PlanningPriority Priority { get; init; } = PlanningPriority.Normal;

    /// <summary>First local calendar day, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Optional local start time, as <c>HH:mm</c>.</summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>Optional local end time, as <c>HH:mm</c>.</summary>
    public TimeOnly? EndTime { get; init; }

    /// <summary>Recurrence values. Omitting them means the item happens once.</summary>
    public SaveRecurrenceRequest? Recurrence { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public SavePlanningItemInput ToInput() =>
        new(
            Title,
            Description,
            Kind,
            Category,
            Priority,
            StartDate,
            StartTime,
            EndTime,
            Recurrence?.ToInput());
}

/// <summary>
/// The decision a client is recording about one occurrence.
/// </summary>
/// <remarks>
/// The occurrence is addressed by the item identifier and the date in the route, so the body
/// carries nothing but the decision itself and cannot be used to reach another day or another item.
/// </remarks>
public sealed class SetOccurrenceStatusRequest
{
    /// <summary>What the user decided.</summary>
    public OccurrenceStatus Status { get; init; } = OccurrenceStatus.Planned;
}
