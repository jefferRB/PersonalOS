using PersonalOS.Application.Calendar;
using PersonalOS.Application.Journal;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Routines;
using PersonalOS.Application.Study;
using PersonalOS.Application.Time;
using PersonalOS.Domain.Planning;

namespace PersonalOS.Application.Today;

/// <summary>
/// Builds the integrated view of one local day from every daily module.
/// </summary>
/// <remarks>
/// <para>
/// The service composes the existing feature services rather than owning its own queries. Each
/// module keeps one place where its data is read and validated, and Today gains no second copy of
/// those rules. In particular the day's occurrences come from <see cref="CalendarService"/>, so
/// planning a day and executing it read exactly one projection.
/// </para>
/// <para>
/// One request performs a fixed, small number of queries: calendar items and occurrence states for
/// the day, active workout routines, routine sessions for the day, the nutrition goal, meals for
/// the day, study sessions for the day, study project names, and the dates that hold a journal
/// entry. The count does not grow with the amount of data, so there is no N+1 query hiding here.
/// </para>
/// <para>
/// Which day "today" is comes from the account's persisted time zone and the application clock,
/// never from the browser. A client may ask for a specific day, and the service still reports what
/// the account's real current day is so the screen can label it correctly.
/// </para>
/// </remarks>
public sealed class TodayService(
    TimeContextService timeContextService,
    CalendarService calendarService,
    RoutineService routineService,
    NutritionService nutritionService,
    StudyService studyService,
    JournalService journalService)
{
    /// <summary>
    /// Builds the Today view for one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="requestedDate">
    /// Local calendar day to show. When <see langword="null"/>, the account's current local day
    /// is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<TodaySummaryRecord> GetAsync(
        Guid userId,
        DateOnly? requestedDate,
        CancellationToken cancellationToken)
    {
        var localTime = await timeContextService.GetAsync(userId, cancellationToken);
        var date = requestedDate ?? localTime.LocalDate;

        var occurrences = await calendarService.GetOccurrencesAsync(
            userId,
            date,
            date,
            cancellationToken);

        var routines = await routineService.GetOccurrencesAsync(
            userId,
            date,
            date,
            cancellationToken);

        var nutrition = await nutritionService.GetDayAsync(userId, date, cancellationToken);

        var studyResult = await studyService.GetSessionsAsync(
            userId,
            date,
            date,
            cancellationToken);
        var studySessions = studyResult.Value ?? [];

        var journalDates = await journalService.GetWrittenDatesAsync(
            userId,
            date,
            date,
            cancellationToken);

        var progress = new TodayProgressRecord(
            occurrences.Count(occurrence => occurrence.Status != OccurrenceStatus.Cancelled),
            occurrences.Count(occurrence => occurrence.Status == OccurrenceStatus.Completed),
            routines.Count,
            routines.Count(routine => routine.IsCompleted),
            studySessions.Sum(session => session.DurationMinutes),
            nutrition.ConsumedCalories,
            nutrition.Goal.DailyCalorieTarget,
            journalDates.Count > 0);

        return new TodaySummaryRecord(
            date,
            localTime.TimeZoneId,
            date == localTime.LocalDate,
            TimeOnly.FromTimeSpan(localTime.LocalNow.TimeOfDay),
            occurrences,
            routines,
            nutrition,
            studySessions,
            progress);
    }
}
