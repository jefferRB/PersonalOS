using PersonalOS.Application.Calendar;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Routines;
using PersonalOS.Application.Study;

namespace PersonalOS.Application.Today;

/// <summary>
/// Counts that describe how one local day is going.
/// </summary>
/// <param name="PlannedItemCount">How many occurrences are on the day and not cancelled.</param>
/// <param name="CompletedItemCount">How many of them are finished.</param>
/// <param name="RoutineCount">How many workout routines apply to the day.</param>
/// <param name="CompletedRoutineCount">How many of those routines were finished.</param>
/// <param name="StudyMinutes">Minutes of studying recorded.</param>
/// <param name="ConsumedCalories">Calories recorded.</param>
/// <param name="DailyCalorieTarget">The target the user chose, or <see langword="null"/>.</param>
/// <param name="JournalCompleted">Whether the day's reflection holds any text.</param>
/// <remarks>
/// Every value is counted from data the user actually entered. There is deliberately no streak,
/// no score, and no trend: those would be invented until real history exists, and an invented
/// number in a personal record is worse than no number at all.
/// </remarks>
public sealed record TodayProgressRecord(
    int PlannedItemCount,
    int CompletedItemCount,
    int RoutineCount,
    int CompletedRoutineCount,
    int StudyMinutes,
    int ConsumedCalories,
    int? DailyCalorieTarget,
    bool JournalCompleted);

/// <summary>
/// Everything the Today screen shows for one local calendar day.
/// </summary>
/// <param name="LocalDate">The local calendar day being shown.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="IsToday">Whether that day is the account's current local day.</param>
/// <param name="LocalTimeOfDay">
/// The account's current local time. It comes from the application clock and the saved time zone,
/// so the timeline can mark "now" without trusting the browser clock.
/// </param>
/// <param name="Occurrences">
/// Calendar occurrences for the day, produced by the calendar's own projection.
/// </param>
/// <param name="Routines">Workout routines that apply to the day, with their execution state.</param>
/// <param name="Nutrition">Meals and calorie totals for the day.</param>
/// <param name="StudySessions">Study recorded for the day.</param>
/// <param name="Progress">Counts describing how the day is going.</param>
/// <remarks>
/// Today reads the calendar's occurrence projection rather than a second task model of its own.
/// Planning a day and executing it are two views of the same rows, and keeping two shapes in step
/// would be a permanent source of disagreement between the screens.
/// </remarks>
public sealed record TodaySummaryRecord(
    DateOnly LocalDate,
    string TimeZoneId,
    bool IsToday,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceRecord> Occurrences,
    IReadOnlyList<RoutineOccurrenceRecord> Routines,
    NutritionDayRecord Nutrition,
    IReadOnlyList<StudySessionRecord> StudySessions,
    TodayProgressRecord Progress);
