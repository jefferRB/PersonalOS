namespace PersonalOS.Domain.Routines;

/// <summary>
/// Decides which local calendar days a routine applies to.
/// </summary>
/// <remarks>
/// <para>
/// Occurrences are <em>calculated</em>, never generated in advance. PersonalOS stores one rule per
/// routine and evaluates <see cref="OccursOn"/> for the days a screen actually shows. Writing rows
/// for future days would multiply storage, would need a background job to extend the horizon, and
/// would leave stale rows behind whenever a rule changed. A row is written only when the user
/// really executes a routine, as a routine session.
/// </para>
/// <para>
/// Every value is a local calendar date. A rule is therefore independent of instants and of
/// daylight-saving transitions: "every Monday" stays every Monday.
/// </para>
/// </remarks>
public sealed class RecurrenceRule
{
    /// <summary>Smallest accepted interval.</summary>
    public const int MinInterval = 1;

    /// <summary>
    /// Largest accepted interval.
    /// </summary>
    /// <remarks>
    /// The cap is a sanity limit rather than a business rule. It rejects values that could only
    /// come from a typing mistake or an abusive client.
    /// </remarks>
    public const int MaxInterval = 365;

    /// <summary>Bitmask value with every weekday selected.</summary>
    public const int AllWeekdaysMask = 0b111_1111;

    private RecurrenceRule()
    {
    }

    /// <summary>Repetition pattern.</summary>
    public RecurrenceFrequency Frequency { get; private set; }

    /// <summary>How many days, weeks, or months separate two occurrences.</summary>
    public int Interval { get; private set; } = MinInterval;

    /// <summary>First local calendar day the rule can produce.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Last local calendar day the rule can produce, or <see langword="null"/>.</summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>
    /// Chosen weekdays, stored as a bitmask where Sunday is bit 0 and Saturday is bit 6.
    /// </summary>
    /// <remarks>
    /// A bitmask keeps the rule inside one row and one column. A child table of weekdays would
    /// add a join to every calendar query to express seven booleans.
    /// </remarks>
    public int SelectedWeekdaysMask { get; private set; }

    /// <summary>
    /// Reports whether an interval is inside the accepted range.
    /// </summary>
    /// <param name="interval">Candidate interval.</param>
    public static bool IsIntervalValid(int interval) =>
        interval is >= MinInterval and <= MaxInterval;

    /// <summary>
    /// Reports whether a weekday bitmask is usable for the given frequency.
    /// </summary>
    /// <param name="frequency">Repetition pattern.</param>
    /// <param name="selectedWeekdaysMask">Candidate bitmask.</param>
    public static bool IsWeekdayMaskValid(RecurrenceFrequency frequency, int selectedWeekdaysMask)
    {
        if (selectedWeekdaysMask is < 0 or > AllWeekdaysMask)
        {
            return false;
        }

        // Choosing "selected weekdays" without selecting any weekday would produce a routine that
        // never happens, which is never what the user meant.
        return frequency != RecurrenceFrequency.SelectedWeekdays || selectedWeekdaysMask != 0;
    }

    /// <summary>
    /// Reports whether an end date can follow a start date.
    /// </summary>
    /// <param name="startDate">First day of the series.</param>
    /// <param name="endDate">Optional last day of the series.</param>
    public static bool IsDateRangeValid(DateOnly startDate, DateOnly? endDate) =>
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
        Enum.GetValues<DayOfWeek>()
            .Where(weekday => (mask & (1 << (int)weekday)) != 0)
            .ToArray();

    /// <summary>
    /// Creates a rule that happens once.
    /// </summary>
    /// <param name="startDate">The single local calendar day.</param>
    public static RecurrenceRule Once(DateOnly startDate) =>
        Create(RecurrenceFrequency.None, MinInterval, startDate, endDate: null, selectedWeekdaysMask: 0);

    /// <summary>
    /// Creates a validated rule.
    /// </summary>
    /// <param name="frequency">Repetition pattern.</param>
    /// <param name="interval">Distance between occurrences.</param>
    /// <param name="startDate">First local calendar day.</param>
    /// <param name="endDate">Optional last local calendar day.</param>
    /// <param name="selectedWeekdaysMask">Weekday bitmask, used by <see cref="RecurrenceFrequency.SelectedWeekdays"/>.</param>
    /// <exception cref="ArgumentException">A value cannot produce a usable series.</exception>
    public static RecurrenceRule Create(
        RecurrenceFrequency frequency,
        int interval,
        DateOnly startDate,
        DateOnly? endDate,
        int selectedWeekdaysMask)
    {
        if (!IsIntervalValid(interval))
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                $"The interval must be between {MinInterval} and {MaxInterval}.");
        }

        if (!IsDateRangeValid(startDate, endDate))
        {
            throw new ArgumentException(
                "The end date cannot be before the start date.",
                nameof(endDate));
        }

        if (!IsWeekdayMaskValid(frequency, selectedWeekdaysMask))
        {
            throw new ArgumentException(
                "Select at least one weekday for a selected-weekdays routine.",
                nameof(selectedWeekdaysMask));
        }

        return new RecurrenceRule
        {
            Frequency = frequency,
            Interval = interval,
            StartDate = startDate,
            EndDate = endDate,
            // Weekdays only mean something for one frequency. Clearing the mask otherwise keeps
            // stored rules comparable and prevents a hidden value from reappearing after an edit.
            SelectedWeekdaysMask = frequency == RecurrenceFrequency.SelectedWeekdays
                ? selectedWeekdaysMask
                : 0,
        };
    }

    /// <summary>
    /// Reports whether the routine applies to one local calendar day.
    /// </summary>
    /// <param name="date">Local calendar day being tested.</param>
    /// <remarks>
    /// The calculation is pure: the same rule and the same date always give the same answer, on
    /// any host, in any time zone, at any moment.
    /// </remarks>
    public bool OccursOn(DateOnly date)
    {
        if (date < StartDate)
        {
            return false;
        }

        if (EndDate is not null && date > EndDate.Value)
        {
            return false;
        }

        return Frequency switch
        {
            RecurrenceFrequency.None => date == StartDate,
            RecurrenceFrequency.Daily => (date.DayNumber - StartDate.DayNumber) % Interval == 0,
            RecurrenceFrequency.Weekly => date.DayOfWeek == StartDate.DayOfWeek
                && WeeksSinceStart(date) % Interval == 0,
            RecurrenceFrequency.SelectedWeekdays => IsWeekdaySelected(date.DayOfWeek)
                && WeeksSinceStart(date) % Interval == 0,
            RecurrenceFrequency.Monthly => MonthsSinceStart(date) % Interval == 0
                && date.Day == DayOfMonthIn(date),
            _ => false,
        };
    }

    /// <summary>
    /// Lists the local calendar days the routine applies to inside an inclusive range.
    /// </summary>
    /// <param name="from">First day to test.</param>
    /// <param name="to">Last day to test.</param>
    /// <remarks>
    /// The caller decides the window, which is what keeps the calculation bounded. The screens
    /// ask for one day or one month, never for an open-ended series.
    /// </remarks>
    public IEnumerable<DateOnly> OccurrencesBetween(DateOnly from, DateOnly to)
    {
        var first = from > StartDate ? from : StartDate;
        var last = EndDate is not null && EndDate.Value < to ? EndDate.Value : to;

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            if (OccursOn(date))
            {
                yield return date;
            }
        }
    }

    private bool IsWeekdaySelected(DayOfWeek weekday) =>
        (SelectedWeekdaysMask & (1 << (int)weekday)) != 0;

    /// <summary>
    /// Whole weeks between the week containing the start date and the week containing a date.
    /// </summary>
    /// <remarks>
    /// Weeks are anchored to Monday so that "every two weeks on Monday and Wednesday" keeps both
    /// days inside the same repetition, instead of splitting the week across two cycles.
    /// </remarks>
    private int WeeksSinceStart(DateOnly date) =>
        (StartOfWeek(date).DayNumber - StartOfWeek(StartDate).DayNumber) / 7;

    private int MonthsSinceStart(DateOnly date) =>
        ((date.Year - StartDate.Year) * 12) + (date.Month - StartDate.Month);

    /// <summary>
    /// The start day-of-month clamped to the length of the tested month.
    /// </summary>
    /// <remarks>
    /// A routine that starts on the 31st still happens in February. Clamping to the last day of
    /// the shorter month is the least surprising behaviour and never skips a month silently.
    /// </remarks>
    private int DayOfMonthIn(DateOnly date) =>
        Math.Min(StartDate.Day, DateTime.DaysInMonth(date.Year, date.Month));

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;

        return date.AddDays(-offset);
    }
}
