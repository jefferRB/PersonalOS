using PersonalOS.Application.Calendar;
using PersonalOS.Application.Journal;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Profile;
using PersonalOS.Application.Routines;
using PersonalOS.Application.Study;
using PersonalOS.Domain.Journal;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Domain.Planning;
using PersonalOS.Domain.Routines;
using PersonalOS.Domain.Study;

namespace PersonalOS.UnitTests.Daily;

/// <summary>
/// In-memory implementations of the daily persistence ports.
/// </summary>
/// <remarks>
/// <para>
/// These let the application services be tested without a database, which keeps the unit suite
/// fast and independent of SQL Server. They deliberately reproduce the one behaviour that matters
/// for correctness here: every read and write is filtered by the account identifier, so a test
/// that accidentally leaked another account's data would fail here too.
/// </para>
/// <para>
/// They are not a second implementation of the product. Query shapes, indexes, and cascade rules
/// are verified against the real database by the integration and migration checks.
/// </para>
/// </remarks>
public sealed class InMemoryCalendarStore : ICalendarStore
{
    private readonly List<PlanningItem> items = [];
    private readonly List<PlanningItemOccurrenceState> states = [];

    /// <summary>How many state rows exist, so a test can prove nothing was written.</summary>
    public int StateCount => states.Count;

    public Task<IReadOnlyList<PlanningItem>> GetItemsOverlappingAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlanningItem>>(
        [
            .. items
                .Where(item => item.UserId == userId
                    && item.StartDate <= to
                    && (item.Recurrence.EndDate is null || item.Recurrence.EndDate >= from))
                .OrderBy(item => item.StartDate)
                .ThenBy(item => item.Title)
        ]);

    public Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesInRangeAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlanningItemOccurrenceState>>(
        [
            .. states.Where(state => state.UserId == userId
                && state.OccurrenceDate >= from
                && state.OccurrenceDate <= to)
        ]);

    public Task<PlanningItem?> FindItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        Task.FromResult(items.FirstOrDefault(
            item => item.Id == itemId && item.UserId == userId));

    public Task<bool> HasOccurrenceStatesAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        Task.FromResult(states.Any(
            state => state.PlanningItemId == itemId && state.UserId == userId));

    public Task<PlanningItemOccurrenceState?> FindStateAsync(
        Guid userId,
        Guid itemId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(states.FirstOrDefault(state =>
            state.PlanningItemId == itemId
            && state.UserId == userId
            && state.OccurrenceDate == occurrenceDate));

    public Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesForItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlanningItemOccurrenceState>>(
        [
            .. states.Where(state =>
                state.PlanningItemId == itemId && state.UserId == userId)
        ]);

    public Task AddItemAsync(PlanningItem item, CancellationToken cancellationToken)
    {
        items.Add(item);

        return Task.CompletedTask;
    }

    public Task AddStateAsync(
        PlanningItemOccurrenceState state,
        CancellationToken cancellationToken)
    {
        states.Add(state);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> DeleteItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var removed = items.RemoveAll(item => item.Id == itemId && item.UserId == userId) > 0;

        if (removed)
        {
            // The real store relies on the database cascade, which the in-memory one has to do by
            // hand or a deleted series would leave its decisions behind.
            states.RemoveAll(state => state.PlanningItemId == itemId);
        }

        return Task.FromResult(removed);
    }
}

/// <inheritdoc cref="InMemoryCalendarStore" />
public sealed class InMemoryRoutineStore : IRoutineStore
{
    private readonly List<RoutineTemplate> templates = [];
    private readonly List<RoutineSession> sessions = [];

    public Task<IReadOnlyList<RoutineTemplate>> GetTemplatesAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RoutineTemplate>>(
        [
            .. templates
                .Where(template => template.UserId == userId && (!activeOnly || template.IsActive))
                .OrderBy(template => template.Name)
        ]);

    public Task<RoutineTemplate?> FindTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken) =>
        Task.FromResult(templates.FirstOrDefault(
            template => template.Id == templateId && template.UserId == userId));

    public Task AddTemplateAsync(RoutineTemplate template, CancellationToken cancellationToken)
    {
        templates.Add(template);

        return Task.CompletedTask;
    }

    public Task SaveTemplateAsync(RoutineTemplate template, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> DeleteTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var removed = templates.RemoveAll(
            template => template.Id == templateId && template.UserId == userId);

        // The database cascades sessions from their routine; the fake reproduces that so a test
        // cannot pass here and fail against SQL Server.
        sessions.RemoveAll(session => session.RoutineTemplateId == templateId);

        return Task.FromResult(removed > 0);
    }

    public Task<IReadOnlyList<RoutineSession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RoutineSession>>(
        [
            .. sessions.Where(session => session.UserId == userId
                && session.LocalDate >= from
                && session.LocalDate <= to)
        ]);

    public Task<RoutineSession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(sessions.FirstOrDefault(
            session => session.Id == sessionId && session.UserId == userId));

    public Task<RoutineSession?> FindSessionForDateAsync(
        Guid userId,
        Guid templateId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(sessions.FirstOrDefault(session => session.UserId == userId
            && session.RoutineTemplateId == templateId
            && session.LocalDate == localDate));

    public Task AddSessionAsync(RoutineSession session, CancellationToken cancellationToken)
    {
        sessions.Add(session);

        return Task.CompletedTask;
    }

    public Task SaveSessionAsync(RoutineSession session, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <inheritdoc cref="InMemoryPlanningStore" />
public sealed class InMemoryNutritionStore : INutritionStore
{
    private readonly Dictionary<Guid, NutritionGoal> goals = [];
    private readonly List<MealEntry> meals = [];

    public Task<NutritionGoal?> FindGoalAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(goals.GetValueOrDefault(userId));

    public Task<NutritionGoal> SaveGoalAsync(
        Guid userId,
        int dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (goals.TryGetValue(userId, out var existing))
        {
            existing.Update(
                dailyCalorieTarget,
                proteinTargetGrams,
                carbohydrateTargetGrams,
                fatTargetGrams,
                utcNow);

            return Task.FromResult(existing);
        }

        var goal = NutritionGoal.Create(
            userId,
            dailyCalorieTarget,
            proteinTargetGrams,
            carbohydrateTargetGrams,
            fatTargetGrams,
            utcNow);
        goals[userId] = goal;

        return Task.FromResult(goal);
    }

    public Task<IReadOnlyList<MealEntry>> GetMealsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MealEntry>>(
        [
            .. meals
                .Where(meal => meal.UserId == userId
                    && meal.LocalDate >= from
                    && meal.LocalDate <= to)
                .OrderBy(meal => meal.OccurredAtLocalTime)
        ]);

    public Task<MealEntry?> FindMealAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken) =>
        Task.FromResult(meals.FirstOrDefault(
            meal => meal.Id == mealId && meal.UserId == userId));

    public Task AddMealAsync(MealEntry entry, CancellationToken cancellationToken)
    {
        meals.Add(entry);

        return Task.CompletedTask;
    }

    public Task SaveMealAsync(MealEntry entry, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> DeleteMealAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken) =>
        Task.FromResult(meals.RemoveAll(meal => meal.Id == mealId && meal.UserId == userId) > 0);
}

/// <inheritdoc cref="InMemoryPlanningStore" />
public sealed class InMemoryStudyStore : IStudyStore
{
    private readonly List<StudyProject> projects = [];
    private readonly List<StudySession> sessions = [];

    public Task<IReadOnlyList<StudyProject>> GetProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StudyProject>>(
            [.. projects.Where(project => project.UserId == userId).OrderBy(project => project.Name)]);

    public Task<StudyProject?> FindProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken) =>
        Task.FromResult(projects.FirstOrDefault(
            project => project.Id == projectId && project.UserId == userId));

    public Task AddProjectAsync(StudyProject project, CancellationToken cancellationToken)
    {
        projects.Add(project);

        return Task.CompletedTask;
    }

    public Task SaveProjectAsync(StudyProject project, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<StudySession>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StudySession>>(
        [
            .. sessions
                .Where(session => session.UserId == userId
                    && session.LocalDate >= from
                    && session.LocalDate <= to)
                .OrderBy(session => session.LocalDate)
        ]);

    public Task<StudySession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(sessions.FirstOrDefault(
            session => session.Id == sessionId && session.UserId == userId));

    public Task AddSessionAsync(StudySession session, CancellationToken cancellationToken)
    {
        sessions.Add(session);

        return Task.CompletedTask;
    }

    public Task SaveSessionAsync(StudySession session, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> DeleteSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(sessions.RemoveAll(
            session => session.Id == sessionId && session.UserId == userId) > 0);
}

/// <inheritdoc cref="InMemoryPlanningStore" />
public sealed class InMemoryJournalStore : IJournalStore
{
    private readonly List<DailyJournalEntry> entries = [];

    public Task<DailyJournalEntry?> FindAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(entries.FirstOrDefault(
            entry => entry.UserId == userId && entry.LocalDate == localDate));

    public Task<IReadOnlyList<DateOnly>> GetWrittenDatesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DateOnly>>(
        [
            .. entries
                .Where(entry => entry.UserId == userId
                    && entry.LocalDate >= from
                    && entry.LocalDate <= to
                    && entry.HasContent)
                .Select(entry => entry.LocalDate)
        ]);

    public Task AddAsync(DailyJournalEntry entry, CancellationToken cancellationToken)
    {
        // The database enforces one entry per account per local day with a unique index. The fake
        // enforces it too, so a service that started creating duplicates would fail here.
        if (entries.Any(item => item.UserId == entry.UserId && item.LocalDate == entry.LocalDate))
        {
            throw new InvalidOperationException(
                "An entry already exists for this account and local date.");
        }

        entries.Add(entry);

        return Task.CompletedTask;
    }

    public Task SaveAsync(DailyJournalEntry entry, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <summary>How many entries exist in total, used to prove that saving twice does not duplicate.</summary>
    public int Count => entries.Count;
}

/// <summary>
/// Profile store that reports one fixed time zone, so Today tests stay deterministic.
/// </summary>
public sealed class FixedTimeZoneProfileStore(string timeZoneId) : IUserProfileStore
{
    public Task<UserProfileRecord?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<UserProfileRecord?>(new UserProfileRecord(
            "Jefferson",
            "user@example.com",
            timeZoneId,
            CalendarDisplayRecord.Default,
            DateTimeOffset.UnixEpoch));

    public Task<UserProfileRecord?> SaveAsync(
        Guid userId,
        string displayName,
        string newTimeZoneId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        GetAsync(userId, cancellationToken);

    public Task<UserProfileRecord?> SaveCalendarDisplayAsync(
        Guid userId,
        CalendarDisplayRecord display,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        GetAsync(userId, cancellationToken);

    public Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(timeZoneId);

    public Task EnsurePreferencesAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
