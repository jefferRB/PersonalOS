namespace PersonalOS.Domain.Routines;

/// <summary>
/// Repetition patterns supported by this milestone.
/// </summary>
/// <remarks>
/// The set is deliberately small. Full RFC 5545 support brings exception dates, positional rules
/// such as "the third Tuesday", time-zone-aware expansion, and an editing model for "this
/// occurrence versus the whole series". None of that is needed to plan a week, and each piece
/// would need its own tests to be trustworthy.
/// </remarks>
public enum RecurrenceFrequency
{
    /// <summary>Happens once, on the start date.</summary>
    None = 0,

    /// <summary>Every <c>Interval</c> days from the start date.</summary>
    Daily = 1,

    /// <summary>Every <c>Interval</c> weeks, on the same weekday as the start date.</summary>
    Weekly = 2,

    /// <summary>Every <c>Interval</c> weeks, on each chosen weekday.</summary>
    SelectedWeekdays = 3,

    /// <summary>Every <c>Interval</c> months, on the same day number as the start date.</summary>
    Monthly = 4,
}
