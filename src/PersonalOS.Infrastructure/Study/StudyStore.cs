using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Study;
using PersonalOS.Domain.Study;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Study;

/// <summary>
/// EF Core implementation of <see cref="IStudyStore"/>.
/// </summary>
public sealed class StudyStore(ApplicationDbContext dbContext) : IStudyStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<StudyProject>> GetProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.StudyProjects
            .AsNoTracking()
            .Include(project => project.Resources)
            .Where(project => project.UserId == userId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<StudyProject?> FindProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken) =>
        await dbContext.StudyProjects
            .Include(project => project.Resources)
            .FirstOrDefaultAsync(
                project => project.Id == projectId && project.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddProjectAsync(StudyProject project, CancellationToken cancellationToken)
    {
        dbContext.StudyProjects.Add(project);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveProjectAsync(StudyProject project, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudySession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.StudySessions
            .AsNoTracking()
            .Where(session => session.UserId == userId
                && session.LocalDate >= from
                && session.LocalDate <= to)
            .OrderBy(session => session.LocalDate)
            .ThenBy(session => session.StartTime)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<StudySession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        await dbContext.StudySessions
            .FirstOrDefaultAsync(
                session => session.Id == sessionId && session.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddSessionAsync(StudySession session, CancellationToken cancellationToken)
    {
        dbContext.StudySessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveSessionAsync(StudySession session, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.StudySessions
            .Where(session => session.Id == sessionId && session.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
