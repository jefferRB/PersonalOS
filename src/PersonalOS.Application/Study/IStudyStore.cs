using PersonalOS.Domain.Study;

namespace PersonalOS.Application.Study;

/// <summary>
/// Persistence port for study projects and study sessions.
/// </summary>
public interface IStudyStore
{
    /// <summary>
    /// Reads the projects of one account, with their resources.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<StudyProject>> GetProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one project owned by an account, with its resources.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StudyProject?> FindProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new project.
    /// </summary>
    /// <param name="project">Project created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddProjectAsync(StudyProject project, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to a project previously returned by <see cref="FindProjectAsync"/>.
    /// </summary>
    /// <param name="project">Project that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveProjectAsync(StudyProject project, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the sessions recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<StudySession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one session owned by an account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StudySession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new session.
    /// </summary>
    /// <param name="session">Session created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddSessionAsync(StudySession session, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to a session previously returned by <see cref="FindSessionAsync"/>.
    /// </summary>
    /// <param name="session">Session that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSessionAsync(StudySession session, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes one session owned by an account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
