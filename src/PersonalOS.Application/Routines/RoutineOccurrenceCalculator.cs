using PersonalOS.Domain.Routines;

namespace PersonalOS.Application.Routines;

/// <summary>
/// Turns routine rules plus recorded sessions into the occurrences a screen should show.
/// </summary>
/// <remarks>
/// <para>
/// This is the place where the "calculated, not generated" decision becomes visible. Nothing here
/// touches the database: the caller loads the account's routines and the sessions that already
/// exist for the window, and this class decides which days each routine applies to and which of
/// those days already carry progress.
/// </para>
/// <para>
/// Because the method is pure, the recurrence behaviour can be tested exhaustively without a
/// database, a clock, or a host.
/// </para>
/// </remarks>
public static class RoutineOccurrenceCalculator
{
    /// <summary>
    /// Calculates the occurrences of every routine inside an inclusive local-date range.
    /// </summary>
    /// <param name="templates">Routines owned by the account.</param>
    /// <param name="sessions">Sessions already recorded inside the same range.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <returns>Occurrences ordered by day and then by routine name.</returns>
    public static IReadOnlyList<RoutineOccurrenceRecord> Calculate(
        IReadOnlyList<RoutineTemplate> templates,
        IReadOnlyList<RoutineSession> sessions,
        DateOnly from,
        DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(sessions);

        // One lookup avoids scanning the session list once per routine per day.
        var sessionsByRoutineAndDate = sessions.ToDictionary(
            session => (session.RoutineTemplateId, session.LocalDate));

        var occurrences = new List<RoutineOccurrenceRecord>();

        foreach (var template in templates)
        {
            if (!template.IsActive)
            {
                continue;
            }

            foreach (var date in template.Recurrence.OccurrencesBetween(from, to))
            {
                sessionsByRoutineAndDate.TryGetValue((template.Id, date), out var session);

                occurrences.Add(new RoutineOccurrenceRecord(
                    template.Id,
                    template.Name,
                    template.Category,
                    date,
                    template.Steps.Count,
                    session?.Id,
                    session?.IsCompleted ?? false,
                    session?.StepResults.Count(result => result.IsCompleted) ?? 0));
            }
        }

        return
        [
            .. occurrences
                .OrderBy(occurrence => occurrence.LocalDate)
                .ThenBy(occurrence => occurrence.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }
}
