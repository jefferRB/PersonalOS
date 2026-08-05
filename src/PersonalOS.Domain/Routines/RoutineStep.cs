using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Routines;

/// <summary>
/// One ordered step inside a routine template.
/// </summary>
/// <remarks>
/// A step describes the <em>target</em>: three sets of ten repetitions at sixty kilograms. What
/// actually happened on a given day is recorded separately as a
/// <see cref="RoutineStepResult"/>, so editing a routine never rewrites past sessions.
/// </remarks>
public sealed class RoutineStep
{
    /// <summary>Maximum stored length of the step title.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Maximum stored length of the step notes.</summary>
    public const int NotesMaxLength = 1000;

    /// <summary>Largest accepted number of sets or repetitions.</summary>
    public const int MaxCount = 1000;

    /// <summary>Largest accepted weight, in the unit the user works in.</summary>
    public const decimal MaxWeight = 2000m;

    /// <summary>Largest accepted duration, in minutes.</summary>
    public const int MaxDurationMinutes = 24 * 60;

    private RoutineStep()
    {
    }

    /// <summary>Identifier of this step.</summary>
    public Guid Id { get; private set; }

    /// <summary>Routine template this step belongs to.</summary>
    public Guid RoutineTemplateId { get; private set; }

    /// <summary>
    /// Zero-based position inside the routine.
    /// </summary>
    /// <remarks>
    /// The template renumbers its steps whenever the list changes, so positions stay dense and
    /// unique. Storing gaps would let two steps claim the same place after a reorder.
    /// </remarks>
    public int Order { get; private set; }

    /// <summary>What the user should do, for example <c>Bench press</c>.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Which fields this step expects.</summary>
    public RoutineStepType StepType { get; private set; }

    /// <summary>Target number of sets, for an exercise step.</summary>
    public int? TargetSets { get; private set; }

    /// <summary>Target repetitions per set, for an exercise step.</summary>
    public int? TargetRepetitions { get; private set; }

    /// <summary>Target weight, for an exercise step.</summary>
    public decimal? TargetWeight { get; private set; }

    /// <summary>Target duration in minutes, for a timed step.</summary>
    public int? TargetDurationMinutes { get; private set; }

    /// <summary>Optional guidance shown while executing the routine.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Reports whether a count is inside the accepted range.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    public static bool IsCountValid(int? value) => value is null or (> 0 and <= MaxCount);

    /// <summary>
    /// Reports whether a weight is inside the accepted range.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <remarks>
    /// Zero is accepted so that a body-weight exercise can be recorded honestly.
    /// </remarks>
    public static bool IsWeightValid(decimal? value) => value is null or (>= 0m and <= MaxWeight);

    /// <summary>
    /// Reports whether a duration is inside the accepted range.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    public static bool IsDurationValid(int? value) =>
        value is null or (> 0 and <= MaxDurationMinutes);

    /// <summary>
    /// Creates a step.
    /// </summary>
    /// <param name="order">Zero-based position inside the routine.</param>
    /// <param name="title">What the user should do.</param>
    /// <param name="stepType">Which fields this step expects.</param>
    /// <param name="targetSets">Target number of sets.</param>
    /// <param name="targetRepetitions">Target repetitions per set.</param>
    /// <param name="targetWeight">Target weight.</param>
    /// <param name="targetDurationMinutes">Target duration in minutes.</param>
    /// <param name="notes">Optional guidance.</param>
    public static RoutineStep Create(
        int order,
        string? title,
        RoutineStepType stepType,
        int? targetSets,
        int? targetRepetitions,
        decimal? targetWeight,
        int? targetDurationMinutes,
        string? notes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        if (!IsCountValid(targetSets))
        {
            throw new ArgumentOutOfRangeException(nameof(targetSets));
        }

        if (!IsCountValid(targetRepetitions))
        {
            throw new ArgumentOutOfRangeException(nameof(targetRepetitions));
        }

        if (!IsWeightValid(targetWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(targetWeight));
        }

        if (!IsDurationValid(targetDurationMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(targetDurationMinutes));
        }

        var step = new RoutineStep
        {
            Id = Guid.NewGuid(),
            Order = order,
            Title = TextRules.NormalizeRequiredOrThrow(title, 1, TitleMaxLength, nameof(title)),
            StepType = stepType,
            Notes = TextRules.NormalizeOptionalOrThrow(notes, NotesMaxLength, nameof(notes)),
        };

        step.ApplyTargets(
            stepType,
            targetSets,
            targetRepetitions,
            targetWeight,
            targetDurationMinutes);

        return step;
    }

    internal void MoveTo(int order)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        Order = order;
    }

    internal void AttachTo(Guid routineTemplateId) => RoutineTemplateId = routineTemplateId;

    /// <summary>
    /// Keeps only the targets that belong to the chosen step type.
    /// </summary>
    /// <remarks>
    /// A checklist step that silently kept a stale weight from an earlier edit would display
    /// numbers the user never intended.
    /// </remarks>
    private void ApplyTargets(
        RoutineStepType stepType,
        int? targetSets,
        int? targetRepetitions,
        decimal? targetWeight,
        int? targetDurationMinutes)
    {
        TargetSets = stepType == RoutineStepType.Exercise ? targetSets : null;
        TargetRepetitions = stepType == RoutineStepType.Exercise ? targetRepetitions : null;
        TargetWeight = stepType == RoutineStepType.Exercise ? targetWeight : null;
        TargetDurationMinutes = stepType == RoutineStepType.Timed ? targetDurationMinutes : null;
    }
}
