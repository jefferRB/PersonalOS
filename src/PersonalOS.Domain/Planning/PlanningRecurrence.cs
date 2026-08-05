namespace PersonalOS.Domain.Planning;

/// <summary>
/// Decides which local calendar days one calendar item applies to.
/// </summary>
/// <remarks>
/// <para>
/// Occurrences are <em>calculated</em>, never written in advance. PersonalOS stores one rule per
/// item and asks it for the days a screen is actually showing. Materializing future days would
/// multiply storage, need a background job to extend the horizon, and leave stale rows behind
/// every time a rule changed. The only row a recurrence ever writes is a
/// <see cref="PlanningItemOccurrenceState"/>, and only once the user acts on a specific day.
/// </para>
/// <para>
/// Every value is a local calendar date, so the rule is independent of instants and of
/// daylight-saving transitions: "every Monday" stays every Monday.
/// </para>
/// </remarks>
public sealed class PlanningRecurrence
{
    /// <summary>Smallest accepted interval.</summary>
    public const int MinInterval = 1;

    /// <summary>
    /// Largest accepted interval.
    /// </summary>
    /// <remarks>
    /// The cap is a sanity limit rather than a business rule. It rejects values that could only come
    /// from a typing mistake or an abusive client, and it bounds the work any expansion can do.
    /// </remarks>
    public const int MaxInterval = 365;

    /// <summary>Bitmask value with every weekday selected.</summary>
    public const int AllWeekdaysMask = 0b111_1111;

    private PlanningRecurrence()
    {
    }

    /// <summary>How often the item repeats.</summary>
    public PlanningRecurrenceFrequency Frequency { get; private set; }

    /// <summary>How many days, weeks, or months separate two occurrences.</summary>
    public int Interval { get; private set; } = MinInterval;

    /// <summary>Last local calendar day the rule may produce, or <see langword="null"/> for open-ended.</summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>
    /// Chosen weekdays for a weekly rule, as a bitmask where Sunday is bit 0 and Saturday is bit 6.
    /// </summary>
    /// <remarks>
    /// A bitmask keeps the rule inside one row and one column. A child table of weekdays would add a
    /// join to every calendar query in order to express seven booleans.
    /// </remarks>
    public int SelectedWeekdaysMask { get; private set; }

    /// <summary>Whether this rule produces more than the item's start date.</summary>
    public bool Repeats => Frequency != PlanningRecurrenceFrequency.None;

    /// <summary>
    /// Reports whether an interval is inside the accepted range.
    /// </summary>
    /// <param name="interval">Candidate interval.</param>
    public static bool IsIntervalValid(int interval) =>
        interval is >= MinInterval and <= MaxInterval;

    /// <summary>
    /// Reports whether a weekday bitmask is usable at all.
    /// </summary>
    /// <param name="selectedWeekdaysMask">Candidate bitmask.</param>
    /// <remarks>
    /// An empty mask is accepted: a weekly rule with no weekday chosen falls back to the weekday of
    /// its own start date, which is what a user who picked "every week" and nothing else meant.
    /// </remarks>
    public static bool IsWeekdayMaskValid(int selectedWeekdaysMask) =>
        selectedWeekdaysMask is >= 0 and <= AllWeekdaysMask;

    /// <summary>
    /// Reports whether an end date can follow a start date.
    /// </summary>
    /// <param name="startDate">First day of the series.</param>
    /// <param name="endDate">Optional last day of the series.</param>
    public static bool IsEndDateValid(DateOnly startDate, DateOnly? endDate) =>
        endDate is null || endDate.Value >= startDate;

    /// <summary>
    /// Converts a set of weekdays into the stored bitmask.
    /// </summary>
    /// <param name="weekdays">Chosen weekdays.</param>
    public static int ToMask(IEnumerable<DayOfWeek> weekdays)
    {
        ArgumentNullException.ThrowIfNull(weekdays);

        return weekdays.Aggregate(0, (mask, weekday) => mask | (1 << (int)weekday));
    }

    /// <summary>
    /// Expands the stored bitmask into weekdays, ordered from Sunday to Saturday.
    /// </summary>
    /// <param name="mask">Stored bitmask.</param>
    public static IReadOnlyList<DayOfWeek> FromMask(int mask) =>
        [.. Enum.GetValues<DayOfWeek>().Where(weekday => (mask & (1 << (int)weekday)) != 0)];

    /// <summary>Creates the rule of an item that happens once.</summary>
    public static PlanningRecurrence Once() =>
        new()
        {
            Frequency = PlanningRecurrenceFrequency.None,
            Interval = MinInterval,
            EndDate = null,
            SelectedWeekdaysMask = 0,
        };

    /// <summary>
    /// Creates a validated rule.
    /// </summary>
    /// <param name="frequency">How often the item repeats.</param>
    /// <param name="interval">Distance between occurrences.</param>
    /// <param name="endDate">Optional last local calendar day.</param>
    /// <param name="selectedWeekdaysMask">Weekday bitmask, used by a weekly rule.</param>
    /// <exception cref="ArgumentException">A value cannot produce a usable series.</exception>
    public static PlanningRecurrence Create(
        PlanningRecurrenceFrequency frequency,
        int interval,
        DateOnly? endDate,
        int selectedWeekdaysMask)
    {
        if (frequency == PlanningRecurrenceFrequency.None)
        {
            return Once();
        }

        if (!IsIntervalValid(interval))
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                $"The interval must be between {MinInterval} and {MaxInterval}.");
        }

        if (!IsWeekdayMaskValid(selectedWeekdaysMask))
        {
            throw new ArgumentException(
                "The weekday selection is not a valid combination of weekdays.",
                nameof(selectedWeekdaysMask));
        }

        return new PlanningRecurrence
        {
            Frequency = frequency,
            Interval = interval,
            EndDate = endDate,
            // Weekdays only mean something for a weekly rule. Clearing the mask otherwise keeps
            // stored rules comparable and stops a hidden value from reappearing after an edit.
            SelectedWeekdaysMask = frequency == PlanningRecurrenceFrequency.Weekly
                ? selectedWeekdaysMask
                : 0,
        };
    }

    /// <summary>
    /// Reports whether two rules describe the same repetition, ignoring the end date.
    /// </summary>
    /// <param name="other">Rule to compare against.</param>
    /// <remarks>
    /// The end date is excluded on purpose. Ending a series early is an ordinary edit; changing how
    /// often it repeats would move every occurrence and orphan the days the user already acted on.
    /// </remarks>
    public bool HasSamePattern(PlanningRecurrence other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Frequency == other.Frequency
            && Interval == other.Interval
            && SelectedWeekdaysMask == other.SelectedWeekdaysMask;
    }

    /// <summary>
    /// Reports whether an end date may replace this rule's end date once occurrences were acted on.
    /// </summary>
    /// <param name="candidate">Proposed end date.</param>
    /// <remarks>
    /// A series may be ended or shortened, because that only removes days the user has not reached
    /// yet. Extending or reopening an ended series is refused while stored occurrence states exist,
    /// since the days beyond the old end were never part of what the user was looking at.
    /// </remarks>
    public bool AllowsEndDateChangeTo(DateOnly? candidate)
    {
        if (candidate == EndDate)
        {
            return true;
        }

        // Ending an open-ended series is always allowed; clearing an end date is not.
        return candidate is not null && (EndDate is null || candidate.Value <= EndDate.Value);
    }

    /// <summary>
    /// Reports whether the item applies to one local calendar day.
    /// </summary>
    /// <param name="startDate">The item's first local calendar day.</param>
    /// <param name="date">Local calendar day being tested.</param>
    /// <remarks>
    /// The calculation is pure: the same rule, start date, and date always give the same answer, on
    /// any host, in any time zone, at any moment.
    /// </remarks>
    public bool OccursOn(DateOnly startDate, DateOnly date)
    {
        if (date < startDate)
        {
            return false;
        }

        if (EndDate is not null && date > EndDate.Value)
        {
            return false;
        }

        return Frequency switch
        {
            PlanningRecurrenceFrequency.None => date == startDate,
            PlanningRecurrenceFrequency.Daily =>
                (date.DayNumber - startDate.DayNumber) % Interval == 0,
            PlanningRecurrenceFrequency.Weekly =>
                IsWeekdaySelected(startDate, date.DayOfWeek)
                && WeeksSinceStart(startDate, date) % Interval == 0,
            PlanningRecurrenceFrequency.Monthly =>
                date.Day == startDate.Day
                && MonthsSinceStart(startDate, date) % Interval == 0,
            _ => false,
        };
    }

    /// <summary>
    /// Lists the local calendar days the item applies to inside an inclusive range.
    /// </summary>
    /// <param name="startDate">The item's first local calendar day.</param>
    /// <param name="from">First day to test.</param>
    /// <param name="to">Last day to test.</param>
    /// <remarks>
    /// The caller decides the window, which is what keeps the calculation bounded. Screens ask for
    /// one month, one day, or one week, never for an open-ended series.
    /// </remarks>
    public IEnumerable<DateOnly> OccurrencesBetween(DateOnly startDate, DateOnly from, DateOnly to)
    {
        var first = from > startDate ? from : startDate;
        var last = EndDate is not null && EndDate.Value < to ? EndDate.Value : to;

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            if (OccursOn(startDate, date))
            {
                yield return date;
            }
        }
    }

    /// <summary>
    /// Whether a weekday is part of a weekly rule.
    /// </summary>
    /// <remarks>
    /// An empty selection means the user chose "every week" without naming a day, so the series
    /// follows the weekday of its own start date.
    /// </remarks>
    private bool IsWeekdaySelected(DateOnly startDate, DayOfWeek weekday) =>
        SelectedWeekdaysMask == 0
            ? weekday == startDate.DayOfWeek
            : (SelectedWeekdaysMask & (1 << (int)weekday)) != 0;

    /// <summary>
    /// Whole weeks between the week holding the start date and the week holding a date.
    /// </summary>
    /// <remarks>
    /// Weeks are anchored to Monday so that "every two weeks on Monday and Wednesday" keeps both
    /// days inside the same repetition instead of splitting the week across two cycles.
    /// </remarks>
    private static int WeeksSinceStart(DateOnly startDate, DateOnly date) =>
        (StartOfWeek(date).DayNumber - StartOfWeek(startDate).DayNumber) / 7;

    private static int MonthsSinceStart(DateOnly startDate, DateOnly date) =>
        ((date.Year - startDate.Year) * 12) + (date.Month - startDate.Month);

    private static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
