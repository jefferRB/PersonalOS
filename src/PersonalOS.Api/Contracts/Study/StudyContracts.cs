using PersonalOS.Application.Study;
using PersonalOS.Domain.Study;

namespace PersonalOS.Api.Contracts.Study;

/// <summary>
/// A reference to study material.
/// </summary>
/// <param name="Id">Resource identifier.</param>
/// <param name="Title">How the user named the material.</param>
/// <param name="ResourceType">What kind of material it is.</param>
/// <param name="ExternalUrl">
/// Optional address. Always <c>http</c> or <c>https</c>: the server rejects every other scheme
/// before storing, and never requests the address itself.
/// </param>
/// <param name="Notes">Optional note.</param>
public sealed record StudyResourceResponse(
    Guid Id,
    string Title,
    StudyResourceType ResourceType,
    string? ExternalUrl,
    string? Notes)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static StudyResourceResponse FromRecord(StudyResourceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new StudyResourceResponse(
            record.Id,
            record.Title,
            record.ResourceType,
            record.ExternalUrl,
            record.Notes);
    }
}

/// <summary>
/// A subject or learning project with its material.
/// </summary>
/// <param name="Id">Project identifier.</param>
/// <param name="Name">Subject name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Status">Where the project stands.</param>
/// <param name="Resources">Material attached to the project.</param>
public sealed record StudyProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    StudyProjectStatus Status,
    IReadOnlyList<StudyResourceResponse> Resources)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static StudyProjectResponse FromRecord(StudyProjectRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new StudyProjectResponse(
            record.Id,
            record.Name,
            record.Description,
            record.Status,
            [.. record.Resources.Select(StudyResourceResponse.FromRecord)]);
    }
}

/// <summary>
/// One recorded block of studying.
/// </summary>
/// <param name="Id">Session identifier.</param>
/// <param name="StudyProjectId">Project that was studied.</param>
/// <param name="ProjectName">Name of that project.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="DurationMinutes">How long the session lasted.</param>
/// <param name="Summary">What was studied.</param>
/// <param name="ProgressNote">Where the user now stands.</param>
public sealed record StudySessionResponse(
    Guid Id,
    Guid StudyProjectId,
    string ProjectName,
    DateOnly LocalDate,
    TimeOnly? StartTime,
    int DurationMinutes,
    string? Summary,
    string? ProgressNote)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static StudySessionResponse FromRecord(StudySessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new StudySessionResponse(
            record.Id,
            record.StudyProjectId,
            record.ProjectName,
            record.LocalDate,
            record.StartTime,
            record.DurationMinutes,
            record.Summary,
            record.ProgressNote);
    }
}

/// <summary>
/// Values a client may send for one study resource.
/// </summary>
public sealed class StudyResourceRequest
{
    /// <summary>How the user named the material.</summary>
    public string? Title { get; init; }

    /// <summary>What kind of material it is.</summary>
    public StudyResourceType ResourceType { get; init; } = StudyResourceType.Other;

    /// <summary>Optional address. Only <c>http</c> and <c>https</c> are accepted.</summary>
    public string? ExternalUrl { get; init; }

    /// <summary>Optional note.</summary>
    public string? Notes { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public StudyResourceInput ToInput() => new(Title, ResourceType, ExternalUrl, Notes);
}

/// <summary>
/// Values a client may send when creating or editing a study project.
/// </summary>
public sealed class SaveStudyProjectRequest
{
    /// <summary>Subject name.</summary>
    public string? Name { get; init; }

    /// <summary>Optional longer text.</summary>
    public string? Description { get; init; }

    /// <summary>Where the project stands.</summary>
    public StudyProjectStatus Status { get; init; } = StudyProjectStatus.Active;

    /// <summary>Material attached to the project.</summary>
    public IReadOnlyList<StudyResourceRequest>? Resources { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public StudyProjectInput ToInput() =>
        new(
            Name,
            Description,
            Status,
            Resources is null ? null : [.. Resources.Select(resource => resource.ToInput())]);
}

/// <summary>
/// Values a client may send when recording or editing a study session.
/// </summary>
public sealed class SaveStudySessionRequest
{
    /// <summary>
    /// Project that was studied.
    /// </summary>
    /// <remarks>
    /// The server confirms the project belongs to the authenticated account before saving, so
    /// naming another account's project cannot attach a session to it.
    /// </remarks>
    public Guid? StudyProjectId { get; init; }

    /// <summary>The owner's local calendar day, as <c>yyyy-MM-dd</c>.</summary>
    public DateOnly? LocalDate { get; init; }

    /// <summary>Optional local start time, as <c>HH:mm</c>.</summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>How long the session lasted, in minutes.</summary>
    public int? DurationMinutes { get; init; }

    /// <summary>What was studied.</summary>
    public string? Summary { get; init; }

    /// <summary>Where the user now stands.</summary>
    public string? ProgressNote { get; init; }

    /// <summary>Converts the request into the application input record.</summary>
    public StudySessionInput ToInput() =>
        new(StudyProjectId, LocalDate, StartTime, DurationMinutes, Summary, ProgressNote);
}
