using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Study;

/// <summary>
/// One block of studying recorded on one local calendar day.
/// </summary>
public sealed class StudySession
{
    /// <summary>Maximum stored length of the session summary.</summary>
    public const int SummaryMaxLength = 1000;

    /// <summary>Maximum stored length of the progress note.</summary>
    public const int ProgressNoteMaxLength = 1000;

    /// <summary>Smallest duration that can be recorded, in minutes.</summary>
    public const int MinDurationMinutes = 1;

    /// <summary>
    /// Largest duration that can be recorded, in minutes.
    /// </summary>
    /// <remarks>
    /// A session belongs to one local day, so it cannot be longer than that day.
    /// </remarks>
    public const int MaxDurationMinutes = 24 * 60;

    private StudySession()
    {
    }

    /// <summary>Identifier of this session.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Project that was studied.</summary>
    public Guid StudyProjectId { get; private set; }

    /// <summary>The owner's local calendar day this session belongs to.</summary>
    public DateOnly LocalDate { get; private set; }

    /// <summary>Optional local start time.</summary>
    public TimeOnly? StartTime { get; private set; }

    /// <summary>How long the session lasted, in minutes.</summary>
    public int DurationMinutes { get; private set; }

    /// <summary>What was studied.</summary>
    public string? Summary { get; private set; }

    /// <summary>Where the user now stands, in their own words.</summary>
    public string? ProgressNote { get; private set; }

    /// <summary>Instant the session was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the session was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Reports whether a duration is inside the accepted range.
    /// </summary>
    /// <param name="durationMinutes">Candidate duration.</param>
    public static bool IsDurationValid(int durationMinutes) =>
        durationMinutes is >= MinDurationMinutes and <= MaxDurationMinutes;

    /// <summary>
    /// Records a study session.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="studyProjectId">Project that was studied.</param>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="startTime">Optional local start time.</param>
    /// <param name="durationMinutes">How long the session lasted.</param>
    /// <param name="summary">What was studied.</param>
    /// <param name="progressNote">Where the user now stands.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static StudySession Create(
        Guid userId,
        Guid studyProjectId,
        DateOnly localDate,
        TimeOnly? startTime,
        int durationMinutes,
        string? summary,
        string? progressNote,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (studyProjectId == Guid.Empty)
        {
            throw new ArgumentException(
                "A study project identifier is required.",
                nameof(studyProjectId));
        }

        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StudyProjectId = studyProjectId,
            CreatedAtUtc = utcNow.ToUniversalTime(),
        };

        session.Update(
            studyProjectId,
            localDate,
            startTime,
            durationMinutes,
            summary,
            progressNote,
            utcNow);

        return session;
    }

    /// <summary>
    /// Applies an edit.
    /// </summary>
    /// <param name="studyProjectId">Project that was studied.</param>
    /// <param name="localDate">The owner's local calendar day.</param>
    /// <param name="startTime">Optional local start time.</param>
    /// <param name="durationMinutes">How long the session lasted.</param>
    /// <param name="summary">What was studied.</param>
    /// <param name="progressNote">Where the user now stands.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(
        Guid studyProjectId,
        DateOnly localDate,
        TimeOnly? startTime,
        int durationMinutes,
        string? summary,
        string? progressNote,
        DateTimeOffset utcNow)
    {
        if (!IsDurationValid(durationMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        }

        StudyProjectId = studyProjectId;
        LocalDate = localDate;
        StartTime = startTime;
        DurationMinutes = durationMinutes;
        Summary = TextRules.NormalizeOptionalOrThrow(summary, SummaryMaxLength, nameof(summary));
        ProgressNote = TextRules.NormalizeOptionalOrThrow(
            progressNote,
            ProgressNoteMaxLength,
            nameof(progressNote));
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
