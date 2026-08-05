using PersonalOS.Domain.Study;

namespace PersonalOS.Application.Study;

/// <summary>
/// A reference to study material.
/// </summary>
/// <param name="Id">Resource identifier.</param>
/// <param name="Title">How the user named the material.</param>
/// <param name="ResourceType">What kind of material it is.</param>
/// <param name="ExternalUrl">Optional <c>http</c> or <c>https</c> address.</param>
/// <param name="Notes">Optional note.</param>
public sealed record StudyResourceRecord(
    Guid Id,
    string Title,
    StudyResourceType ResourceType,
    string? ExternalUrl,
    string? Notes)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="resource">Domain entity.</param>
    public static StudyResourceRecord FromEntity(StudyResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new StudyResourceRecord(
            resource.Id,
            resource.Title,
            resource.ResourceType,
            resource.ExternalUrl,
            resource.Notes);
    }
}

/// <summary>
/// Values a client may supply for one study resource.
/// </summary>
/// <param name="Title">How the user named the material.</param>
/// <param name="ResourceType">What kind of material it is.</param>
/// <param name="ExternalUrl">Optional web address.</param>
/// <param name="Notes">Optional note.</param>
public sealed record StudyResourceInput(
    string? Title,
    StudyResourceType ResourceType,
    string? ExternalUrl,
    string? Notes);

/// <summary>
/// A subject or learning project with its material.
/// </summary>
/// <param name="Id">Project identifier.</param>
/// <param name="Name">Subject name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Status">Where the project stands.</param>
/// <param name="Resources">Material attached to the project.</param>
public sealed record StudyProjectRecord(
    Guid Id,
    string Name,
    string? Description,
    StudyProjectStatus Status,
    IReadOnlyList<StudyResourceRecord> Resources)
{
    /// <summary>
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="project">Domain entity.</param>
    public static StudyProjectRecord FromEntity(StudyProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new StudyProjectRecord(
            project.Id,
            project.Name,
            project.Description,
            project.Status,
            [.. project.Resources.Select(StudyResourceRecord.FromEntity)]);
    }
}

/// <summary>
/// Values a client may supply when creating or editing a study project.
/// </summary>
/// <param name="Name">Subject name.</param>
/// <param name="Description">Optional longer text.</param>
/// <param name="Status">Where the project stands.</param>
/// <param name="Resources">Material attached to the project.</param>
public sealed record StudyProjectInput(
    string? Name,
    string? Description,
    StudyProjectStatus Status,
    IReadOnlyList<StudyResourceInput>? Resources);

/// <summary>
/// One recorded block of studying.
/// </summary>
/// <param name="Id">Session identifier.</param>
/// <param name="StudyProjectId">Project that was studied.</param>
/// <param name="ProjectName">Name of that project, so the client needs no second request.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="DurationMinutes">How long the session lasted.</param>
/// <param name="Summary">What was studied.</param>
/// <param name="ProgressNote">Where the user now stands.</param>
public sealed record StudySessionRecord(
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
    /// Projects a domain entity onto the application record.
    /// </summary>
    /// <param name="session">Domain entity.</param>
    /// <param name="projectName">Name of the project the session belongs to.</param>
    public static StudySessionRecord FromEntity(StudySession session, string projectName)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new StudySessionRecord(
            session.Id,
            session.StudyProjectId,
            projectName,
            session.LocalDate,
            session.StartTime,
            session.DurationMinutes,
            session.Summary,
            session.ProgressNote);
    }
}

/// <summary>
/// Values a client may supply when recording or editing a study session.
/// </summary>
/// <param name="StudyProjectId">Project that was studied.</param>
/// <param name="LocalDate">The owner's local calendar day.</param>
/// <param name="StartTime">Optional local start time.</param>
/// <param name="DurationMinutes">How long the session lasted.</param>
/// <param name="Summary">What was studied.</param>
/// <param name="ProgressNote">Where the user now stands.</param>
public sealed record StudySessionInput(
    Guid? StudyProjectId,
    DateOnly? LocalDate,
    TimeOnly? StartTime,
    int? DurationMinutes,
    string? Summary,
    string? ProgressNote);
