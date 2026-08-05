using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Routines;

/// <summary>
/// What actually happened for one step during one routine session.
/// </summary>
/// <remarks>
/// Results are separate from steps so that history stays truthful. Changing a routine's target
/// weight next month must not rewrite the weight that was lifted last week.
/// </remarks>
public sealed class RoutineStepResult
{
    /// <summary>Maximum stored length of the result notes.</summary>
    public const int NotesMaxLength = 1000;

    private RoutineStepResult()
    {
    }

    /// <summary>Identifier of this result.</summary>
    public Guid Id { get; private set; }

    /// <summary>Session this result belongs to.</summary>
    public Guid RoutineSessionId { get; private set; }

    /// <summary>Step this result describes.</summary>
    public Guid RoutineStepId { get; private set; }

    /// <summary>Whether the user finished the step.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Sets actually performed.</summary>
    public int? ActualSets { get; private set; }

    /// <summary>Repetitions actually performed.</summary>
    public int? ActualRepetitions { get; private set; }

    /// <summary>Weight actually used.</summary>
    public decimal? ActualWeight { get; private set; }

    /// <summary>Minutes actually spent.</summary>
    public int? ActualDurationMinutes { get; private set; }

    /// <summary>Optional note about how the step went.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Creates an empty result for a step.
    /// </summary>
    /// <param name="routineStepId">Step this result describes.</param>
    public static RoutineStepResult ForStep(Guid routineStepId)
    {
        if (routineStepId == Guid.Empty)
        {
            throw new ArgumentException("A step identifier is required.", nameof(routineStepId));
        }

        return new RoutineStepResult
        {
            Id = Guid.NewGuid(),
            RoutineStepId = routineStepId,
        };
    }

    /// <summary>
    /// Records what happened for this step.
    /// </summary>
    /// <param name="isCompleted">Whether the user finished the step.</param>
    /// <param name="actualSets">Sets actually performed.</param>
    /// <param name="actualRepetitions">Repetitions actually performed.</param>
    /// <param name="actualWeight">Weight actually used.</param>
    /// <param name="actualDurationMinutes">Minutes actually spent.</param>
    /// <param name="notes">Optional note.</param>
    /// <remarks>
    /// Every measurement is optional. A user who checks a step without typing numbers has still
    /// told the truth, and inventing values would corrupt their history.
    /// </remarks>
    public void Record(
        bool isCompleted,
        int? actualSets,
        int? actualRepetitions,
        decimal? actualWeight,
        int? actualDurationMinutes,
        string? notes)
    {
        if (!RoutineStep.IsCountValid(actualSets))
        {
            throw new ArgumentOutOfRangeException(nameof(actualSets));
        }

        if (!RoutineStep.IsCountValid(actualRepetitions))
        {
            throw new ArgumentOutOfRangeException(nameof(actualRepetitions));
        }

        if (!RoutineStep.IsWeightValid(actualWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(actualWeight));
        }

        if (!RoutineStep.IsDurationValid(actualDurationMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(actualDurationMinutes));
        }

        IsCompleted = isCompleted;
        ActualSets = actualSets;
        ActualRepetitions = actualRepetitions;
        ActualWeight = actualWeight;
        ActualDurationMinutes = actualDurationMinutes;
        Notes = TextRules.NormalizeOptionalOrThrow(notes, NotesMaxLength, nameof(notes));
    }

    internal void AttachTo(Guid routineSessionId) => RoutineSessionId = routineSessionId;
}
