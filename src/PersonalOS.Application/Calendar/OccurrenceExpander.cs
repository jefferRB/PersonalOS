using PersonalOS.Domain.Planning;

namespace PersonalOS.Application.Calendar;

/// <summary>
/// Turns calendar items plus the days the user acted on into the occurrences a screen should show.
/// </summary>
/// <remarks>
/// <para>
/// This is where "calculated, not materialized" becomes visible. Nothing here touches the database:
/// the caller loads the account's items and the states that already exist for the window, and this
/// class decides which days each item applies to and what the user decided about each of them.
/// </para>
/// <para>
/// Because every method is pure, recurrence behaviour can be tested exhaustively without a database,
/// a clock, or a host.
/// </para>
/// </remarks>
public static class OccurrenceExpander
{
    /// <summary>
    /// Expands every item across an inclusive local-date range.
    /// </summary>
    /// <param name="items">Calendar items owned by the account.</param>
    /// <param name="states">Occurrence states already recorded inside the same range.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <returns>
    /// Occurrences ordered by day, then untimed before timed, then by start time, then by title.
    /// Untimed items sort first because the agenda shows them above the timeline.
    /// </returns>
    public static IReadOnlyList<CalendarOccurrenceRecord> Expand(
        IReadOnlyList<PlanningItem> items,
        IReadOnlyList<PlanningItemOccurrenceState> states,
        DateOnly from,
        DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(states);

        if (to < from)
        {
            return [];
        }

        // One lookup avoids scanning the state list once per item per day.
        var statesByItemAndDate = states.ToDictionary(
            state => (state.PlanningItemId, state.OccurrenceDate));

        var occurrences = new List<CalendarOccurrenceRecord>();

        foreach (var item in items)
        {
            foreach (var date in item.OccurrencesBetween(from, to))
            {
                statesByItemAndDate.TryGetValue((item.Id, date), out var state);

                occurrences.Add(new CalendarOccurrenceRecord(
                    item.Id,
                    date,
                    item.Title,
                    item.Description,
                    item.Kind,
                    item.Category,
                    item.Priority,
                    item.StartTime,
                    item.EndTime,
                    // No stored row means the day is still merely planned, which is what every day
                    // starts as. That is the whole reason nothing is written until the user acts.
                    state?.Status ?? OccurrenceStatus.Planned,
                    item.Recurrence.Repeats,
                    IsImportant(item.Kind, item.Priority),
                    state?.CompletedAtUtc));
            }
        }

        return Sort(occurrences);
    }

    /// <summary>
    /// Reduces a range of occurrences to per-day summaries for a month grid.
    /// </summary>
    /// <param name="occurrences">Occurrences produced by <see cref="Expand"/>.</param>
    /// <param name="maxKindsPerDay">How many distinct kind indicators a cell may advertise.</param>
    /// <remarks>
    /// Titles and descriptions are dropped here rather than at the API boundary, so a month response
    /// cannot accidentally carry private text: the summary type has nowhere to put it.
    /// </remarks>
    public static IReadOnlyList<CalendarDaySummaryRecord> Summarize(
        IReadOnlyList<CalendarOccurrenceRecord> occurrences,
        int maxKindsPerDay)
    {
        ArgumentNullException.ThrowIfNull(occurrences);

        return
        [
            .. occurrences
                .GroupBy(occurrence => occurrence.OccurrenceDate)
                .OrderBy(group => group.Key)
                .Select(group => new CalendarDaySummaryRecord(
                    group.Key,
                    group.Count(),
                    group.Count(occurrence => occurrence.Status == OccurrenceStatus.Completed),
                    group.Count(occurrence => occurrence.Status == OccurrenceStatus.Failed),
                    group.Count(occurrence => occurrence.Status == OccurrenceStatus.Cancelled),
                    [
                        // Cancelled days are counted in the totals but do not advertise a kind: a
                        // cancelled event should not tell the user an event is still happening.
                        // Busiest kind first, so a capped cell drops the least significant one.
                        .. group
                            .Where(occurrence => occurrence.Status != OccurrenceStatus.Cancelled)
                            .GroupBy(occurrence => occurrence.Kind)
                            .Select(kinds => new DayKindCountRecord(kinds.Key, kinds.Count()))
                            .OrderByDescending(kind => kind.Count)
                            .ThenBy(kind => kind.Kind)
                            .Take(maxKindsPerDay)
                    ],
                    group.Any(occurrence =>
                        occurrence.Priority == PlanningPriority.High
                        && occurrence.Status != OccurrenceStatus.Cancelled)))
        ];
    }

    /// <summary>
    /// Reports whether an activity is one the user should not be surprised by.
    /// </summary>
    /// <param name="kind">What sort of thing the activity is.</param>
    /// <param name="priority">How much it matters.</param>
    /// <remarks>
    /// An event or an appointment counts whatever its priority, because both are commitments at a
    /// fixed time and being surprised by one is exactly the failure the next-seven-days section
    /// exists to prevent. A task or a routine is the user's own to reschedule, so it earns a place
    /// only when they marked it important.
    ///
    /// The answer deliberately ignores the occurrence's status. "Important" describes the activity;
    /// whether a particular day was completed or cancelled is a separate question the screens filter
    /// on separately, and folding the two together would make "important only" silently mean
    /// "important and not cancelled".
    /// </remarks>
    public static bool IsImportant(PlanningItemKind kind, PlanningPriority priority) =>
        kind is PlanningItemKind.Event or PlanningItemKind.Appointment
        || priority == PlanningPriority.High;

    /// <summary>
    /// Groups the occurrences of a range by day, dropping days that hold nothing.
    /// </summary>
    /// <param name="occurrences">Occurrences produced by <see cref="Expand"/>.</param>
    public static IReadOnlyList<UpcomingDayRecord> GroupByDay(
        IReadOnlyList<CalendarOccurrenceRecord> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);

        return
        [
            .. occurrences
                .GroupBy(occurrence => occurrence.OccurrenceDate)
                .OrderBy(group => group.Key)
                .Select(group => new UpcomingDayRecord(group.Key, Sort([.. group])))
        ];
    }

    private static IReadOnlyList<CalendarOccurrenceRecord> Sort(
        List<CalendarOccurrenceRecord> occurrences) =>
        [
            .. occurrences
                .OrderBy(occurrence => occurrence.OccurrenceDate)
                .ThenBy(occurrence => occurrence.StartTime.HasValue)
                .ThenBy(occurrence => occurrence.StartTime)
                .ThenBy(occurrence => occurrence.Title, StringComparer.OrdinalIgnoreCase)
        ];
}
