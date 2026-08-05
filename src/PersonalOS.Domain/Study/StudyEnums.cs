namespace PersonalOS.Domain.Study;

/// <summary>
/// Where a study project stands.
/// </summary>
public enum StudyProjectStatus
{
    /// <summary>Currently being studied.</summary>
    Active = 0,

    /// <summary>Set aside for now.</summary>
    Paused = 1,

    /// <summary>Finished.</summary>
    Completed = 2,
}

/// <summary>
/// What kind of material a study resource points to.
/// </summary>
/// <remarks>
/// The type is a label the user chooses. PersonalOS never inspects the target to confirm it, and
/// never downloads it.
/// </remarks>
public enum StudyResourceType
{
    /// <summary>A recording to listen to.</summary>
    Audio = 0,

    /// <summary>A recording to watch.</summary>
    Video = 1,

    /// <summary>A document.</summary>
    Pdf = 2,

    /// <summary>An exam or practice test.</summary>
    Exam = 3,

    /// <summary>An article or written page.</summary>
    Article = 4,

    /// <summary>Anything else.</summary>
    Other = 5,
}
