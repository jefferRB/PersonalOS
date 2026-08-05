using PersonalOS.Domain.Routines;

namespace PersonalOS.Application.Routines;

/// <summary>
/// A recurrence rule, as the application layer hands it to the API.
/// </summary>
/// <param name="Frequency">Repetition pattern.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="StartDate">First local calendar day.</param>
/// <param name="EndDate">Optional last local calendar day.</param>
/// <param name="SelectedWeekdays">Chosen weekdays, from Sunday to Saturday.</param>
/// <remarks>
/// Weekdays travel as a list rather than as the stored bitmask. The bitmask is a storage decision
/// and a client should not have to know about it.
/// </remarks>
public sealed record RecurrenceRuleRecord(
    RecurrenceFrequency Frequency,
    int Interval,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek> SelectedWeekdays)
{
    /// <summary>
    /// Projects a domain rule onto the application record.
    /// </summary>
    /// <param name="rule">Domain rule.</param>
    public static RecurrenceRuleRecord FromEntity(RecurrenceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new RecurrenceRuleRecord(
            rule.Frequency,
            rule.Interval,
            rule.StartDate,
            rule.EndDate,
            RecurrenceRule.FromMask(rule.SelectedWeekdaysMask));
    }
}

/// <summary>
/// Values a client may supply for a recurrence rule.
/// </summary>
/// <param name="Frequency">Repetition pattern.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="StartDate">First local calendar day.</param>
/// <param name="EndDate">Optional last local calendar day.</param>
/// <param name="SelectedWeekdays">Chosen weekdays.</param>
public sealed record RecurrenceInput(
    RecurrenceFrequency Frequency,
    int Interval,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek>? SelectedWeekdays);

/// <summary>
/// One target step of a routine.
/// </summary>
/// <param name="Id">Step identifier.</param>
/// <param name="Order">Zero-based position inside the routine.</param>
/// <param name="Title">What the user should do.</param>
/// <param name="StepType">Which fields this step expects.</param>
/// <param name="TargetSets">Target number of sets.</param>
/// <param name="TargetRepetitions">Target repetitions per set.</param>
/// <param name="TargetWeight">Target weight.</param>
/// <param name="TargetDurationMinutes">Target duration in minutes.</param>
/// <param name="Notes">Optional guidance.</param>
public sealed record RoutineStepRecord(
    Guid Id,
    int Order,
    string Title,
    RoutineStepType StepType,
    int? TargetSets,
    int? TargetRepetitions,
    decimal? TargetWeight,
    int? TargetDurationMinutes,
    string? Notes)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="step">Domain entity.</param>
    public static RoutineStepRecord FromEntity(RoutineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new RoutineStepRecord(
            step.Id,
            step.Order,
            step.Title,
            step.StepType,
            step.TargetSets,
            step.TargetRepetitions,
            step.TargetWeight,
            step.TargetDurationMinutes,
            step.Notes);
    }
}

/// <summary>
/// Values a client may supply for one routine step.
/// </summary>
/// <param name="Title">What the user should do.</param>
/// <param name="StepType">Which fields this step expects.</param>
/// <param name="TargetSets">Target number of sets.</param>
/// <param name="TargetRepetitions">Target repetitions per set.</param>
/// <param name="TargetWeight">Target weight.</param>
/// <param name="TargetDurationMinutes">Target duration in minutes.</param>
/// <param name="Notes">Optional guidance.</param>
/// <remarks>
/// The input carries no identifier and no order. The editor sends the whole list in the order the
/// user arranged it, and the domain renumbers it, so a client cannot claim a position that
/// another step already holds.
/// </remarks>
public sealed record RoutineStepInput(
    string? Title,
    RoutineStepType StepType,
    int? TargetSets,
    int? TargetRepetitions,
    decimal? TargetWeight,
    int? TargetDurationMinutes,
    string? Notes);

/// <summary>
/// A routine template with its ordered steps.
/// </summary>
/// <param name="Id">Routine identifier.</param>
/// <param name="Name">Routine name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="Recurrence">Which local calendar days it applies to.</param>
/// <param name="IsActive">Whether it still appears on Today and on the calendar.</param>
/// <param name="Steps">Ordered steps.</param>
public sealed record RoutineTemplateRecord(
    Guid Id,
    string Name,
    string? Description,
    RoutineCategory Category,
    RecurrenceRuleRecord Recurrence,
    bool IsActive,
    IReadOnlyList<RoutineStepRecord> Steps)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="template">Domain entity.</param>
    public static RoutineTemplateRecord FromEntity(RoutineTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new RoutineTemplateRecord(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            RecurrenceRuleRecord.FromEntity(template.Recurrence),
            template.IsActive,
            [.. template.Steps.OrderBy(step => step.Order).Select(RoutineStepRecord.FromEntity)]);
    }
}

/// <summary>
/// Values a client may supply when creating or editing a routine.
/// </summary>
/// <param name="Name">Routine name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="Recurrence">Recurrence values.</param>
/// <param name="IsActive">Whether it still appears on Today.</param>
/// <param name="Steps">Steps in the order the user arranged them.</param>
public sealed record RoutineTemplateInput(
    string? Name,
    string? Description,
    RoutineCategory Category,
    RecurrenceInput? Recurrence,
    bool IsActive,
    IReadOnlyList<RoutineStepInput>? Steps);

/// <summary>
/// What happened for one step during one session.
/// </summary>
/// <param name="RoutineStepId">Step this result describes.</param>
/// <param name="IsCompleted">Whether the user finished the step.</param>
/// <param name="ActualSets">Sets actually performed.</param>
/// <param name="ActualRepetitions">Repetitions actually performed.</param>
/// <param name="ActualWeight">Weight actually used.</param>
/// <param name="ActualDurationMinutes">Minutes actually spent.</param>
/// <param name="Notes">Optional note.</param>
public sealed record RoutineStepResultRecord(
    Guid RoutineStepId,
    bool IsCompleted,
    int? ActualSets,
    int? ActualRepetitions,
    decimal? ActualWeight,
    int? ActualDurationMinutes,
    string? Notes)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="result">Domain entity.</param>
    public static RoutineStepResultRecord FromEntity(RoutineStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new RoutineStepResultRecord(
            result.RoutineStepId,
            result.IsCompleted,
            result.ActualSets,
            result.ActualRepetitions,
            result.ActualWeight,
            result.ActualDurationMinutes,
            result.Notes);
    }
}

/// <summary>
/// Values a client may supply for one step result.
/// </summary>
/// <param name="RoutineStepId">Step this result describes.</param>
/// <param name="IsCompleted">Whether the user finished the step.</param>
/// <param name="ActualSets">Sets actually performed.</param>
/// <param name="ActualRepetitions">Repetitions actually performed.</param>
/// <param name="ActualWeight">Weight actually used.</param>
/// <param name="ActualDurationMinutes">Minutes actually spent.</param>
/// <param name="Notes">Optional note.</param>
public sealed record RoutineStepResultInput(
    Guid RoutineStepId,
    bool IsCompleted,
    int? ActualSets,
    int? ActualRepetitions,
    decimal? ActualWeight,
    int? ActualDurationMinutes,
    string? Notes);

/// <summary>
/// Values a client may supply when saving progress on a session.
/// </summary>
/// <param name="Notes">Optional note about the session as a whole.</param>
/// <param name="IsCompleted">Whether the user finished the whole routine.</param>
/// <param name="StepResults">Results the user recorded.</param>
public sealed record RoutineSessionInput(
    string? Notes,
    bool IsCompleted,
    IReadOnlyList<RoutineStepResultInput>? StepResults);

/// <summary>
/// One execution of a routine on one local calendar day.
/// </summary>
/// <param name="Id">Session identifier.</param>
/// <param name="RoutineTemplateId">Routine that was executed.</param>
/// <param name="RoutineName">Name of that routine, so the client needs no second request.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="StartedAtUtc">Instant the session was started, in UTC.</param>
/// <param name="CompletedAtUtc">Instant the session was finished, in UTC.</param>
/// <param name="Notes">Optional note about the session as a whole.</param>
/// <param name="Steps">Target steps of the routine, so targets can be shown beside results.</param>
/// <param name="StepResults">What actually happened.</param>
public sealed record RoutineSessionRecord(
    Guid Id,
    Guid RoutineTemplateId,
    string RoutineName,
    RoutineCategory Category,
    DateOnly LocalDate,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Notes,
    IReadOnlyList<RoutineStepRecord> Steps,
    IReadOnlyList<RoutineStepResultRecord> StepResults)
{
    /// <summary>
    /// Projects a session and its routine onto the application record.
    /// </summary>
    /// <param name="session">Session entity.</param>
    /// <param name="template">Routine the session executed.</param>
    public static RoutineSessionRecord FromEntity(RoutineSession session, RoutineTemplate template)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(template);

        return new RoutineSessionRecord(
            session.Id,
            session.RoutineTemplateId,
            template.Name,
            template.Category,
            session.LocalDate,
            session.StartedAtUtc,
            session.CompletedAtUtc,
            session.Notes,
            [.. template.Steps.OrderBy(step => step.Order).Select(RoutineStepRecord.FromEntity)],
            [.. session.StepResults.Select(RoutineStepResultRecord.FromEntity)]);
    }
}

/// <summary>
/// A routine that applies to one local calendar day, with its execution state for that day.
/// </summary>
/// <param name="RoutineTemplateId">Routine identifier.</param>
/// <param name="Name">Routine name.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="LocalDate">The local calendar day this occurrence belongs to.</param>
/// <param name="StepCount">How many steps the routine holds.</param>
/// <param name="SessionId">Session identifier when the user has started this day.</param>
/// <param name="IsCompleted">Whether the session was finished.</param>
/// <param name="CompletedStepCount">How many steps were checked.</param>
/// <remarks>
/// An occurrence with no <paramref name="SessionId"/> exists only as a calculation. Nothing is
/// written until the user actually starts the routine.
/// </remarks>
public sealed record RoutineOccurrenceRecord(
    Guid RoutineTemplateId,
    string Name,
    RoutineCategory Category,
    DateOnly LocalDate,
    int StepCount,
    Guid? SessionId,
    bool IsCompleted,
    int CompletedStepCount);
