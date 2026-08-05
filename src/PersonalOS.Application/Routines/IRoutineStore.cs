using PersonalOS.Domain.Routines;

namespace PersonalOS.Application.Routines;

/// <summary>
/// Persistence port for routine templates and their sessions.
/// </summary>
/// <remarks>
/// Every member takes the account identifier the API derived from the authenticated principal, and
/// implementations must filter by it. Steps and step results are reached only through their
/// parent, so scoping the parent is enough to scope the children.
/// </remarks>
public interface IRoutineStore
{
    /// <summary>
    /// Reads the routines of one account, with their steps.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="activeOnly">Whether to skip deactivated routines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<RoutineTemplate>> GetTemplatesAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one routine owned by an account, with its steps.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RoutineTemplate?> FindTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new routine.
    /// </summary>
    /// <param name="template">Routine created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddTemplateAsync(RoutineTemplate template, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to a routine previously returned by <see cref="FindTemplateAsync"/>.
    /// </summary>
    /// <param name="template">Routine that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveTemplateAsync(RoutineTemplate template, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes one routine and everything that belongs to it.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the sessions recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<RoutineSession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one session owned by an account, with its step results.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RoutineSession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds the session a routine already has on one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="localDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RoutineSession?> FindSessionForDateAsync(
        Guid userId,
        Guid templateId,
        DateOnly localDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new session and its empty step results.
    /// </summary>
    /// <param name="session">Session created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddSessionAsync(RoutineSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to a session previously returned by a find method.
    /// </summary>
    /// <param name="session">Session that was changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSessionAsync(RoutineSession session, CancellationToken cancellationToken);
}
