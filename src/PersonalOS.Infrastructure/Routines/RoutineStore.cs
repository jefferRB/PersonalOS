using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Routines;
using PersonalOS.Domain.Routines;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Routines;

/// <summary>
/// EF Core implementation of <see cref="IRoutineStore"/>.
/// </summary>
/// <remarks>
/// Routines are loaded together with their steps, and sessions together with their results, so a
/// screen that shows ten routines still issues one query rather than eleven.
/// </remarks>
public sealed class RoutineStore(ApplicationDbContext dbContext) : IRoutineStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RoutineTemplate>> GetTemplatesAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RoutineTemplates
            .AsNoTracking()
            .Include(template => template.Steps)
            .Where(template => template.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(template => template.IsActive);
        }

        return await query
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RoutineTemplate?> FindTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken) =>
        await dbContext.RoutineTemplates
            .Include(template => template.Steps)
            .FirstOrDefaultAsync(
                template => template.Id == templateId && template.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddTemplateAsync(
        RoutineTemplate template,
        CancellationToken cancellationToken)
    {
        dbContext.RoutineTemplates.Add(template);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveTemplateAsync(
        RoutineTemplate template,
        CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.RoutineTemplates
            .Where(template => template.Id == templateId && template.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoutineSession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.RoutineSessions
            .AsNoTracking()
            .Include(session => session.StepResults)
            .Where(session => session.UserId == userId
                && session.LocalDate >= from
                && session.LocalDate <= to)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<RoutineSession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        await dbContext.RoutineSessions
            .Include(session => session.StepResults)
            .FirstOrDefaultAsync(
                session => session.Id == sessionId && session.UserId == userId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<RoutineSession?> FindSessionForDateAsync(
        Guid userId,
        Guid templateId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        await dbContext.RoutineSessions
            .Include(session => session.StepResults)
            .FirstOrDefaultAsync(
                session => session.UserId == userId
                    && session.RoutineTemplateId == templateId
                    && session.LocalDate == localDate,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddSessionAsync(RoutineSession session, CancellationToken cancellationToken)
    {
        dbContext.RoutineSessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveSessionAsync(
        RoutineSession session,
        CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
