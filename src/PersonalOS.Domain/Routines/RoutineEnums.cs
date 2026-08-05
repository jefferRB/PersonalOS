namespace PersonalOS.Domain.Routines;

/// <summary>
/// What a routine is mostly about.
/// </summary>
public enum RoutineCategory
{
    /// <summary>A repeating activity that does not fit another group.</summary>
    General = 0,

    /// <summary>Training, for example a Monday chest session.</summary>
    Workout = 1,

    /// <summary>A repeating study block.</summary>
    Study = 2,

    /// <summary>Meal preparation.</summary>
    Meal = 3,

    /// <summary>Rest, reflection, and other wellbeing routines.</summary>
    Wellbeing = 4,
}

/// <summary>
/// What kind of result a routine step expects.
/// </summary>
/// <remarks>
/// The step type decides which fields the editor and the execution screen show. One step table
/// with optional fields is used instead of a table per type, because the differences are a handful
/// of numbers rather than different behaviour.
/// </remarks>
public enum RoutineStepType
{
    /// <summary>Done or not done.</summary>
    Checklist = 0,

    /// <summary>Sets, repetitions, and weight.</summary>
    Exercise = 1,

    /// <summary>A duration in minutes.</summary>
    Timed = 2,

    /// <summary>A reminder that carries no result.</summary>
    Note = 3,
}
