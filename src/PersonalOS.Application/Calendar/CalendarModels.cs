using PersonalOS.Domain.Planning;

namespace PersonalOS.Application.Calendar;

/// <summary>
/// A recurrence rule, as the application layer hands it to the API.
/// </summary>
/// <param name="Frequency">How often the item repeats.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="EndDate">Optional last local calendar day.</param>
/// <param name="SelectedWeekdays">Chosen weekdays, from Sunday to Saturday.</param>
/// <remarks>
/// Weekdays travel as a list rather than as the stored bitmask. The bitmask is a storage decision
/// and a client should not have to know about it.
/// </remarks>
public sealed record PlanningRecurrenceRecord(
    PlanningRecurrenceFrequency Frequency,
    int Interval,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek> SelectedWeekdays)
{
    /// <summary>
    /// Projects a domain rule onto the application record.
    /// </summary>
    /// <param name="recurrence">Domain rule.</param>
    public static PlanningRecurrenceRecord FromEntity(PlanningRecurrence recurrence)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        return new PlanningRecurrenceRecord(
            recurrence.Frequency,
            recurrence.Interval,
            recurrence.EndDate,
            PlanningRecurrence.FromMask(recurrence.SelectedWeekdaysMask));
    }
}

/// <summary>
/// Values a client may supply for a recurrence rule.
/// </summary>
/// <param name="Frequency">How often the item repeats.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="EndDate">Optional last local calendar day.</param>
/// <param name="SelectedWeekdays">Chosen weekdays.</param>
public sealed record PlanningRecurrenceInput(
    PlanningRecurrenceFrequency Frequency,
    int Interval,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek>? SelectedWeekdays);

/// <summary>
/// One calendar item with its rule, as the editor needs it.
/// </summary>
/// <param name="Id">Item identifier.</param>
/// <param name="Title">Short description.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Kind">What sort of thing this is.</param>
/// <param name="Category">Which area of life this belongs to.</param>
/// <param name="Priority">How much this matters.</param>
/// <param name="StartDate">The owner's local calendar day the series starts on.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="EndTime">Optional local end time.</param>
/// <param name="Recurrence">Which local calendar days this item applies to.</param>
/// <param name="IsRecurrencePatternLocked">
/// Whether the repetition can still be changed. It is frozen once the user has acted on a day,
/// so the editor can disable those controls instead of letting the server refuse the save.
/// </param>
/// <remarks>
/// The record deliberately omits <c>UserId</c>. A client that already proved who it is through the
/// authentication cookie gains nothing from being told its own identifier, and leaving it out
/// removes any temptation to send it back as if it were proof of ownership.
/// </remarks>
public sealed record PlanningItemRecord(
    Guid Id,
    string Title,
    string? Description,
    PlanningItemKind Kind,
    PlanningCategory Category,
    PlanningPriority Priority,
    DateOnly StartDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    PlanningRecurrenceRecord Recurrence,
    bool IsRecurrencePatternLocked)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="item">Domain entity.</param>
    /// <param name="isRecurrencePatternLocked">Whether occurrences have already been acted on.</param>
    public static PlanningItemRecord FromEntity(PlanningItem item, bool isRecurrencePatternLocked)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new PlanningItemRecord(
            item.Id,
            item.Title,
            item.Description,
            item.Kind,
            item.Category,
            item.Priority,
            item.StartDate,
            item.StartTime,
            item.EndTime,
            PlanningRecurrenceRecord.FromEntity(item.Recurrence),
            isRecurrencePatternLocked);
    }
}

/// <summary>
/// One calendar item on one local calendar day.
/// </summary>
/// <param name="PlanningItemId">Item this occurrence belongs to.</param>
/// <param name="OccurrenceDate">The local calendar day.</param>
/// <param name="Title">Short description.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Kind">What sort of thing this is.</param>
/// <param name="Category">Which area of life this belongs to.</param>
/// <param name="Priority">How much this matters.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="EndTime">Optional local end time.</param>
/// <param name="Status">What the user decided about this day.</param>
/// <param name="IsRecurring">Whether the item repeats, so the client can say so.</param>
/// <param name="IsImportant">
/// Whether this belongs in the next-seven-days section. Events and appointments always do, because
/// both are commitments at a fixed time; a task or a routine does only when the user marked it high
/// priority. The rule lives on the server so both screens filter on one answer rather than each
/// re-deriving it.
/// </param>
/// <param name="CompletedAtUtc">Instant this occurrence was completed, in UTC.</param>
/// <remarks>
/// An occurrence is a calculation, not a row. Only its <paramref name="Status"/> can come from
/// storage, and only once the user has completed or cancelled that specific day.
/// </remarks>
public sealed record CalendarOccurrenceRecord(
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
    DateTimeOffset? CompletedAtUtc);

/// <summary>
/// How many of one kind fall on a day, so a month cell can show a compact indicator.
/// </summary>
/// <param name="Kind">What sort of thing these are.</param>
/// <param name="Count">How many of them the day holds.</param>
public sealed record DayKindCountRecord(PlanningItemKind Kind, int Count);

/// <summary>
/// What one cell of a month grid needs to render.
/// </summary>
/// <param name="Date">The local calendar day.</param>
/// <param name="TotalCount">How many occurrences fall on the day, cancelled ones included.</param>
/// <param name="CompletedCount">How many of them are finished.</param>
/// <param name="FailedCount">How many were expected and did not happen.</param>
/// <param name="CancelledCount">How many were called off.</param>
/// <param name="Kinds">
/// Which kinds appear on the day and how many of each, busiest first, capped so a cell shows a few
/// indicators rather than a wall of them.
/// </param>
/// <param name="HasHighPriority">Whether anything on the day is marked important.</param>
/// <remarks>
/// The summary deliberately carries no title and no description. A month view shows twelve hundred
/// cells' worth of private text nobody asked to see, and any of it would end up in a response that
/// is far larger than the screen needs.
/// </remarks>
public sealed record CalendarDaySummaryRecord(
    DateOnly Date,
    int TotalCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount,
    IReadOnlyList<DayKindCountRecord> Kinds,
    bool HasHighPriority);

/// <summary>
/// One month of the calendar grid.
/// </summary>
/// <param name="Year">Year being shown.</param>
/// <param name="Month">Month being shown, from 1 to 12.</param>
/// <param name="FromDate">First day covered, which is the Monday the grid starts on.</param>
/// <param name="ToDate">Last day covered, which is the Sunday the grid ends on.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="Days">Summaries for the days that hold anything, in date order.</param>
/// <remarks>
/// Only days that hold something are listed. An empty day needs no row to be drawn as empty, and
/// sending forty-two rows of zeroes to describe an empty month would be pure waste.
/// </remarks>
public sealed record CalendarMonthRecord(
    int Year,
    int Month,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<CalendarDaySummaryRecord> Days);

/// <summary>
/// Everything the daily agenda and the day planner show for one local calendar day.
/// </summary>
/// <param name="Date">The local calendar day being shown.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="LocalTimeOfDay">
/// The account's current local time, so the timeline can mark "now" without trusting the browser
/// clock.
/// </param>
/// <param name="Occurrences">Occurrences on the day, untimed ones first, then in time order.</param>
public sealed record CalendarDayRecord(
    DateOnly Date,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceRecord> Occurrences);

/// <summary>
/// The occurrences of one local calendar day inside the upcoming window.
/// </summary>
/// <param name="Date">The local calendar day.</param>
/// <param name="Occurrences">Everything on that day, untimed first, then in time order.</param>
public sealed record UpcomingDayRecord(
    DateOnly Date,
    IReadOnlyList<CalendarOccurrenceRecord> Occurrences);

/// <summary>
/// The next seven local days.
/// </summary>
/// <param name="FromDate">First day covered.</param>
/// <param name="ToDate">Last day covered.</param>
/// <param name="TodayLocalDate">The account's current local day, decided by the server.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="Days">Days that hold something, in date order. Empty days are omitted.</param>
/// <remarks>
/// Every occurrence in the window is returned, not only the important ones. Seven days hold a
/// bounded amount of data, and returning all of it lets the section's filters run on the client
/// instead of costing a request per click. Each occurrence still carries the server's own
/// <c>IsImportant</c> answer, so the default view shows exactly what it always did.
/// </remarks>
public sealed record UpcomingWeekRecord(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<UpcomingDayRecord> Days);

/// <summary>
/// Values a client may supply when creating or editing a calendar item.
/// </summary>
/// <param name="Title">Short description. Trimmed by the service.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Kind">What sort of thing this is.</param>
/// <param name="Category">Which area of life this belongs to.</param>
/// <param name="Priority">How much this matters.</param>
/// <param name="StartDate">The owner's local calendar day the series starts on.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="EndTime">Optional local end time.</param>
/// <param name="Recurrence">Recurrence values.</param>
public sealed record SavePlanningItemInput(
    string? Title,
    string? Description,
    PlanningItemKind Kind,
    PlanningCategory Category,
    PlanningPriority Priority,
    DateOnly? StartDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    PlanningRecurrenceInput? Recurrence);
