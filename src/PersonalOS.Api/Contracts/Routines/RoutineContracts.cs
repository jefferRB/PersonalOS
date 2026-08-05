using PersonalOS.Application.Routines;
using PersonalOS.Domain.Routines;

namespace PersonalOS.Api.Contracts.Routines;

/// <summary>
/// A recurrence rule, as returned by the routine endpoints.
/// </summary>
/// <param name="Frequency">Repetition pattern.</param>
/// <param name="Interval">Distance between occurrences.</param>
/// <param name="StartDate">First local calendar day.</param>
/// <param name="EndDate">Optional last local calendar day.</param>
/// <param name="SelectedWeekdays">Chosen weekdays, from Sunday to Saturday.</param>
public sealed record RecurrenceResponse(
    RecurrenceFrequency Frequency,
    int Interval,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<DayOfWeek> SelectedWeekdays)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RecurrenceResponse FromRecord(RecurrenceRuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RecurrenceResponse(
            record.Frequency,
            record.Interval,
            record.StartDate,
            record.EndDate,
            record.SelectedWeekdays);
    }
}

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
public sealed record RoutineStepResponse(
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
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RoutineStepResponse FromRecord(RoutineStepRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RoutineStepResponse(
            record.Id,
            record.Order,
            record.Title,
            record.StepType,
            record.TargetSets,
            record.TargetRepetitions,
            record.TargetWeight,
            record.TargetDurationMinutes,
            record.Notes);
    }
}

/// <summary>
/// A routine with its ordered steps.
/// </summary>
/// <param name="Id">Routine identifier.</param>
/// <param name="Name">Routine name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="Recurrence">Which local calendar days it applies to.</param>
/// <param name="IsActive">Whether it still appears on Today and on the calendar.</param>
/// <param name="Steps">Ordered steps.</param>
public sealed record RoutineTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    RoutineCategory Category,
    RecurrenceResponse Recurrence,
    bool IsActive,
    IReadOnlyList<RoutineStepResponse> Steps)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RoutineTemplateResponse FromRecord(RoutineTemplateRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RoutineTemplateResponse(
            record.Id,
            record.Name,
            record.Description,
            record.Category,
            RecurrenceResponse.FromRecord(record.Recurrence),
            record.IsActive,
            [.. record.Steps.Select(RoutineStepResponse.FromRecord)]);
    }
}

/// <summary>
/// What actually happened for one step during one session.
/// </summary>
/// <param name="RoutineStepId">Step this result describes.</param>
/// <param name="IsCompleted">Whether the user finished the step.</param>
/// <param name="ActualSets">Sets actually performed.</param>
/// <param name="ActualRepetitions">Repetitions actually performed.</param>
/// <param name="ActualWeight">Weight actually used.</param>
/// <param name="ActualDurationMinutes">Minutes actually spent.</param>
/// <param name="Notes">Optional note.</param>
public sealed record RoutineStepResultResponse(
    Guid RoutineStepId,
    bool IsCompleted,
    int? ActualSets,
    int? ActualRepetitions,
    decimal? ActualWeight,
    int? ActualDurationMinutes,
    string? Notes)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RoutineStepResultResponse FromRecord(RoutineStepResultRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RoutineStepResultResponse(
            record.RoutineStepId,
            record.IsCompleted,
            record.ActualSets,
            record.ActualRepetitions,
            record.ActualWeight,
            record.ActualDurationMinutes,
            record.Notes);
    }
}

/// <summary>
/// One execution of a routine on one local calendar day.
/// </summary>
/// <param name="Id">Session identifier.</param>
/// <param name="RoutineTemplateId">Routine that was executed.</param>
/// <param name="RoutineName">Name of that routine.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="StartedAtUtc">Instant the session was started, in UTC.</param>
/// <param name="CompletedAtUtc">Instant the session was finished, in UTC.</param>
/// <param name="Notes">Optional note about the session as a whole.</param>
/// <param name="Steps">Target steps, so targets can be displayed beside results.</param>
/// <param name="StepResults">What actually happened.</param>
public sealed record RoutineSessionResponse(
    Guid Id,
    Guid RoutineTemplateId,
    string RoutineName,
    RoutineCategory Category,
    DateOnly LocalDate,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Notes,
    IReadOnlyList<RoutineStepResponse> Steps,
    IReadOnlyList<RoutineStepResultResponse> StepResults)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RoutineSessionResponse FromRecord(RoutineSessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RoutineSessionResponse(
            record.Id,
            record.RoutineTemplateId,
            record.RoutineName,
            record.Category,
            record.LocalDate,
            record.StartedAtUtc,
            record.CompletedAtUtc,
            record.Notes,
            [.. record.Steps.Select(RoutineStepResponse.FromRecord)],
            [.. record.StepResults.Select(RoutineStepResultResponse.FromRecord)]);
    }
}

/// <summary>
/// A routine that applies to one local calendar day, with that day's execution state.
/// </summary>
/// <param name="RoutineTemplateId">Routine identifier.</param>
/// <param name="Name">Routine name.</param>
/// <param name="Category">What the routine is mostly about.</param>
/// <param name="LocalDate">The local calendar day this occurrence belongs to.</param>
/// <param name="StepCount">How many steps the routine holds.</param>
/// <param name="SessionId">Session identifier when the user has started this day.</param>
/// <param name="IsCompleted">Whether the session was finished.</param>
/// <param name="CompletedStepCount">How many steps were checked.</param>
public sealed record RoutineOccurrenceResponse(
    Guid RoutineTemplateId,
    string Name,
    RoutineCategory Category,
    DateOnly LocalDate,
    int StepCount,
    Guid? SessionId,
    bool IsCompleted,
    int CompletedStepCount)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static RoutineOccurrenceResponse FromRecord(RoutineOccurrenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RoutineOccurrenceResponse(
            record.RoutineTemplateId,
            record.Name,
            record.Category,
            record.LocalDate,
            record.StepCount,
            record.SessionId,
            record.IsCompleted,
            record.CompletedStepCount);
    }
}

/// <summary>
/// Values a client may send for a recurrence rule.
/// </summary>
public sealed class RecurrenceRequest
{
    /// <summary>Repetition pattern.</summary>
    public RecurrenceFrequency Frequency { get; init; } = RecurrenceFrequency.None;

    /// <summary>Distance between occurrences. Defaults to every one.</summary>
    public int Interval { get; init; } = 1;

    /// <summary>First local calendar day, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Optional last local calendar day.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Chosen weekdays, used only by the selected-weekdays pattern.</summary>
    public IReadOnlyList<DayOfWeek>? SelectedWeekdays { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public RecurrenceInput ToInput() =>
        new(Frequency, Interval, StartDate, EndDate, SelectedWeekdays);
}

/// <summary>
/// Values a client may send for one routine step.
/// </summary>
/// <remarks>
/// The request carries no identifier and no order. The whole list is sent in the order the user
/// arranged it and the server renumbers it, so a client cannot claim a position or address a step
/// that belongs to a different routine.
/// </remarks>
public sealed class RoutineStepRequest
{
    /// <summary>What the user should do.</summary>
    public string? Title { get; init; }

    /// <summary>Which fields this step expects.</summary>
    public RoutineStepType StepType { get; init; } = RoutineStepType.Checklist;

    /// <summary>Target number of sets.</summary>
    public int? TargetSets { get; init; }

    /// <summary>Target repetitions per set.</summary>
    public int? TargetRepetitions { get; init; }

    /// <summary>Target weight.</summary>
    public decimal? TargetWeight { get; init; }

    /// <summary>Target duration in minutes.</summary>
    public int? TargetDurationMinutes { get; init; }

    /// <summary>Optional guidance.</summary>
    public string? Notes { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public RoutineStepInput ToInput() =>
        new(
            Title,
            StepType,
            TargetSets,
            TargetRepetitions,
            TargetWeight,
            TargetDurationMinutes,
            Notes);
}

/// <summary>
/// Values a client may send when creating or editing a routine.
/// </summary>
public sealed class SaveRoutineRequest
{
    /// <summary>Routine name.</summary>
    public string? Name { get; init; }

    /// <summary>Optional longer text.</summary>
    public string? Description { get; init; }

    /// <summary>What the routine is mostly about.</summary>
    public RoutineCategory Category { get; init; } = RoutineCategory.General;

    /// <summary>Recurrence values.</summary>
    public RecurrenceRequest? Recurrence { get; init; }

    /// <summary>Whether the routine still appears on Today.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Steps in the order the user arranged them.</summary>
    public IReadOnlyList<RoutineStepRequest>? Steps { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public RoutineTemplateInput ToInput() =>
        new(
            Name,
            Description,
            Category,
            Recurrence?.ToInput(),
            IsActive,
            Steps is null ? null : [.. Steps.Select(step => step.ToInput())]);
}

/// <summary>
/// Values a client may send when starting a routine session.
/// </summary>
public sealed class StartRoutineSessionRequest
{
    /// <summary>Local calendar day the session belongs to, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? LocalDate { get; init; }
}

/// <summary>
/// Values a client may send for one step result.
/// </summary>
public sealed class RoutineStepResultRequest
{
    /// <summary>Step this result describes.</summary>
    public Guid RoutineStepId { get; init; }

    /// <summary>Whether the user finished the step.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>Sets actually performed.</summary>
    public int? ActualSets { get; init; }

    /// <summary>Repetitions actually performed.</summary>
    public int? ActualRepetitions { get; init; }

    /// <summary>Weight actually used.</summary>
    public decimal? ActualWeight { get; init; }

    /// <summary>Minutes actually spent.</summary>
    public int? ActualDurationMinutes { get; init; }

    /// <summary>Optional note.</summary>
    public string? Notes { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public RoutineStepResultInput ToInput() =>
        new(
            RoutineStepId,
            IsCompleted,
            ActualSets,
            ActualRepetitions,
            ActualWeight,
            ActualDurationMinutes,
            Notes);
}

/// <summary>
/// Values a client may send when saving progress on a session.
/// </summary>
public sealed class SaveRoutineSessionRequest
{
    /// <summary>Optional note about the session as a whole.</summary>
    public string? Notes { get; init; }

    /// <summary>Whether the user finished the whole routine.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>Results the user recorded.</summary>
    public IReadOnlyList<RoutineStepResultRequest>? StepResults { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public RoutineSessionInput ToInput() =>
        new(
            Notes,
            IsCompleted,
            StepResults is null ? null : [.. StepResults.Select(result => result.ToInput())]);
}
