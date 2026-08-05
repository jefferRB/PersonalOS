using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Routines;

/// <summary>
/// One execution of a routine on one local calendar day.
/// </summary>
/// <remarks>
/// <para>
/// A session is the only row a recurrence ever writes, and it is written when the user actually
/// starts the routine. That is what keeps calculated occurrences cheap: an untouched routine
/// costs nothing, however far into the future the calendar looks.
/// </para>
/// <para>
/// At most one session may exist per routine per local day. The database enforces it, so two
/// browser tabs racing to start the same routine cannot create two histories.
/// </para>
/// </remarks>
public sealed class RoutineSession
{
    /// <summary>Maximum stored length of the session notes.</summary>
    public const int NotesMaxLength = 2000;

    private readonly List<RoutineStepResult> stepResults = [];

    private RoutineSession()
    {
    }

    /// <summary>Identifier of this session.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Routine that was executed.</summary>
    public Guid RoutineTemplateId { get; private set; }

    /// <summary>The owner's local calendar day this session belongs to.</summary>
    public DateOnly LocalDate { get; private set; }

    /// <summary>Instant the session was started, in UTC.</summary>
    public DateTimeOffset StartedAtUtc { get; private set; }

    /// <summary>Instant the session was finished, in UTC, or <see langword="null"/>.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Instant the session was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optional note about the session as a whole.</summary>
    public string? Notes { get; private set; }

    /// <summary>Result recorded for each step.</summary>
    public IReadOnlyList<RoutineStepResult> StepResults => stepResults;

    /// <summary>Whether the user marked the session finished.</summary>
    public bool IsCompleted => CompletedAtUtc is not null;

    /// <summary>
    /// Starts a session, creating one empty result per step.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="template">Routine being executed. Must belong to the same account.</param>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// Results are created up front so that partial progress can be saved without deciding, on
    /// every save, whether a row already exists.
    /// </remarks>
    public static RoutineSession Start(
        Guid userId,
        RoutineTemplate template,
        DateOnly localDate,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (template.UserId != userId)
        {
            throw new ArgumentException(
                "A session can only be started for a routine owned by the same account.",
                nameof(template));
        }

        var startedAt = utcNow.ToUniversalTime();
        var session = new RoutineSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoutineTemplateId = template.Id,
            LocalDate = localDate,
            StartedAtUtc = startedAt,
            UpdatedAtUtc = startedAt,
        };

        foreach (var step in template.Steps)
        {
            var result = RoutineStepResult.ForStep(step.Id);
            result.AttachTo(session.Id);
            session.stepResults.Add(result);
        }

        return session;
    }

    /// <summary>
    /// Finds the result belonging to one step.
    /// </summary>
    /// <param name="routineStepId">Step identifier.</param>
    /// <returns>The result, or <see langword="null"/> when the step is not part of this session.</returns>
    /// <remarks>
    /// A step added to the routine after this session started has no result here. Returning
    /// <see langword="null"/> lets the caller reject the update instead of inventing a row that
    /// belongs to a different version of the routine.
    /// </remarks>
    public RoutineStepResult? FindResult(Guid routineStepId) =>
        stepResults.FirstOrDefault(result => result.RoutineStepId == routineStepId);

    /// <summary>
    /// Records the session-level note.
    /// </summary>
    /// <param name="notes">Optional note.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void RecordNotes(string? notes, DateTimeOffset utcNow)
    {
        Notes = TextRules.NormalizeOptionalOrThrow(notes, NotesMaxLength, nameof(notes));
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Marks the session finished.
    /// </summary>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <returns><see langword="true"/> when this call changed the session.</returns>
    /// <remarks>
    /// Completing an already completed session keeps the original instant, so a repeated request
    /// never rewrites when the work was done.
    /// </remarks>
    public bool Complete(DateTimeOffset utcNow)
    {
        UpdatedAtUtc = utcNow.ToUniversalTime();

        if (CompletedAtUtc is not null)
        {
            return false;
        }

        CompletedAtUtc = UpdatedAtUtc;

        return true;
    }

    /// <summary>
    /// Returns a finished session to in-progress.
    /// </summary>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <returns><see langword="true"/> when this call changed the session.</returns>
    public bool Reopen(DateTimeOffset utcNow)
    {
        UpdatedAtUtc = utcNow.ToUniversalTime();

        if (CompletedAtUtc is null)
        {
            return false;
        }

        CompletedAtUtc = null;

        return true;
    }

    /// <summary>
    /// Records what happened for one step, creating the row when the session lacks it.
    /// </summary>
    /// <param name="routineStepId">Step this result describes.</param>
    /// <param name="isCompleted">Whether the user finished the step.</param>
    /// <param name="actualSets">Sets actually performed.</param>
    /// <param name="actualRepetitions">Repetitions actually performed.</param>
    /// <param name="actualWeight">Weight actually used.</param>
    /// <param name="actualDurationMinutes">Minutes actually spent.</param>
    /// <param name="notes">Optional note.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// A step added to the routine after this session started has no result yet, so one is created
    /// here. The session owns its results, which is why callers record through the session rather
    /// than reaching into the list themselves.
    /// </remarks>
    public void RecordStepResult(
        Guid routineStepId,
        bool isCompleted,
        int? actualSets,
        int? actualRepetitions,
        decimal? actualWeight,
        int? actualDurationMinutes,
        string? notes,
        DateTimeOffset utcNow)
    {
        var result = FindResult(routineStepId);

        if (result is null)
        {
            result = RoutineStepResult.ForStep(routineStepId);
            result.AttachTo(Id);
            stepResults.Add(result);
        }

        result.Record(
            isCompleted,
            actualSets,
            actualRepetitions,
            actualWeight,
            actualDurationMinutes,
            notes);

        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
