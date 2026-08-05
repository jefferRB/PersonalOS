using PersonalOS.Api.Contracts.Calendar;
using PersonalOS.Api.Contracts.Nutrition;
using PersonalOS.Api.Contracts.Routines;
using PersonalOS.Api.Contracts.Study;
using PersonalOS.Application.Today;

namespace PersonalOS.Api.Contracts.Today;

/// <summary>
/// Counts that describe how one local day is going.
/// </summary>
/// <param name="PlannedItemCount">How many occurrences are on the day and not cancelled.</param>
/// <param name="CompletedItemCount">How many of them are finished.</param>
/// <param name="RoutineCount">How many routines apply to the day.</param>
/// <param name="CompletedRoutineCount">How many of those routines were finished.</param>
/// <param name="StudyMinutes">Minutes of studying recorded.</param>
/// <param name="ConsumedCalories">Calories recorded.</param>
/// <param name="DailyCalorieTarget">The target the user chose, or <see langword="null"/>.</param>
/// <param name="JournalCompleted">Whether the day's reflection holds any text.</param>
/// <remarks>
/// Every value is counted from data the user entered. No streak, score, or trend is reported,
/// because none of them could be derived honestly from a single day.
/// </remarks>
public sealed record TodayProgressResponse(
    int PlannedItemCount,
    int CompletedItemCount,
    int RoutineCount,
    int CompletedRoutineCount,
    int StudyMinutes,
    int ConsumedCalories,
    int? DailyCalorieTarget,
    bool JournalCompleted)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static TodayProgressResponse FromRecord(TodayProgressRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new TodayProgressResponse(
            record.PlannedItemCount,
            record.CompletedItemCount,
            record.RoutineCount,
            record.CompletedRoutineCount,
            record.StudyMinutes,
            record.ConsumedCalories,
            record.DailyCalorieTarget,
            record.JournalCompleted);
    }
}

/// <summary>
/// Everything the Today screen shows for one local calendar day.
/// </summary>
/// <param name="LocalDate">The local calendar day being shown, as <c>yyyy-MM-dd</c>.</param>
/// <param name="TimeZoneId">IANA identifier used to decide that day.</param>
/// <param name="IsToday">Whether that day is the account's current local day.</param>
/// <param name="LocalTimeOfDay">
/// The account's current local time, decided by the server so the timeline can mark "now" without
/// trusting the browser clock.
/// </param>
/// <param name="Occurrences">
/// Calendar occurrences for the day, in the same shape the calendar endpoints return, so both
/// screens read one projection and there is no second task model to keep in step.
/// </param>
/// <param name="Routines">Workout routines that apply to the day, with their execution state.</param>
/// <param name="Nutrition">Meals and calorie totals for the day.</param>
/// <param name="StudySessions">Study recorded for the day.</param>
/// <param name="Progress">Counts describing how the day is going.</param>
/// <remarks>
/// The response carries no journal text. Whether the day was reflected on is enough for a
/// summary, and the reflection itself is fetched only by the journal screen.
/// </remarks>
public sealed record TodaySummaryResponse(
    DateOnly LocalDate,
    string TimeZoneId,
    bool IsToday,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceResponse> Occurrences,
    IReadOnlyList<RoutineOccurrenceResponse> Routines,
    NutritionDayResponse Nutrition,
    IReadOnlyList<StudySessionResponse> StudySessions,
    TodayProgressResponse Progress)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static TodaySummaryResponse FromRecord(TodaySummaryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new TodaySummaryResponse(
            record.LocalDate,
            record.TimeZoneId,
            record.IsToday,
            record.LocalTimeOfDay,
            [.. record.Occurrences.Select(CalendarOccurrenceResponse.FromRecord)],
            [.. record.Routines.Select(RoutineOccurrenceResponse.FromRecord)],
            NutritionDayResponse.FromRecord(record.Nutrition),
            [.. record.StudySessions.Select(StudySessionResponse.FromRecord)],
            TodayProgressResponse.FromRecord(record.Progress));
    }
}
