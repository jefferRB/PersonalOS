using PersonalOS.Application.Calendar;
using PersonalOS.Application.Journal;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Routines;
using PersonalOS.Application.Study;
using PersonalOS.Application.Time;
using PersonalOS.Application.Today;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Domain.Planning;
using PersonalOS.Domain.Routines;
using PersonalOS.Domain.Study;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Daily;

/// <summary>
/// The integrated Today view.
/// </summary>
/// <remarks>
/// The tests run on a fixed clock and a fixed time zone, so the local day the service picks is
/// decided entirely by the values under test rather than by the machine running the suite.
/// </remarks>
public sealed class TodayServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly DateOnly LocalDate = new(2026, 7, 30);

    [Fact]
    public async Task AnEmptyDayReportsZeroEverywhereWithoutInventingAnything()
    {
        var context = new TodayContext();

        var summary = await context.GetTodayAsync();

        Assert.Equal(LocalDate, summary.LocalDate);
        Assert.True(summary.IsToday);
        Assert.Empty(summary.Occurrences);
        Assert.Empty(summary.Routines);
        Assert.Empty(summary.StudySessions);
        Assert.Empty(summary.Nutrition.Meals);
        Assert.Equal(0, summary.Progress.PlannedItemCount);
        Assert.Equal(0, summary.Progress.CompletedItemCount);
        Assert.Equal(0, summary.Progress.ConsumedCalories);
        Assert.Null(summary.Progress.DailyCalorieTarget);
        Assert.False(summary.Progress.JournalCompleted);
    }

    [Fact]
    public async Task TheLocalDayComesFromTheSavedTimeZoneNotFromUtc()
    {
        // 00:30 UTC on 31 July is still 30 July in Costa Rica.
        var context = new TodayContext(
            utcNow: new DateTimeOffset(2026, 7, 31, 0, 30, 0, TimeSpan.Zero),
            timeZoneId: "America/Costa_Rica");

        var summary = await context.GetTodayAsync();

        Assert.Equal(new DateOnly(2026, 7, 30), summary.LocalDate);
        Assert.Equal("America/Costa_Rica", summary.TimeZoneId);
    }

    [Fact]
    public async Task TheSameInstantIsAlreadyTheNextDayFurtherEast()
    {
        var context = new TodayContext(
            utcNow: new DateTimeOffset(2026, 7, 30, 23, 30, 0, TimeSpan.Zero),
            timeZoneId: "Asia/Tokyo");

        var summary = await context.GetTodayAsync();

        Assert.Equal(new DateOnly(2026, 7, 31), summary.LocalDate);
    }

    [Fact]
    public async Task TheCurrentLocalTimeIsReportedSoTheTimelineNeedsNoBrowserClock()
    {
        var context = new TodayContext(
            utcNow: new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero),
            timeZoneId: "America/Costa_Rica");

        var summary = await context.GetTodayAsync();

        Assert.Equal(new TimeOnly(13, 24), summary.LocalTimeOfDay);
    }

    [Fact]
    public async Task AskingForAnotherDayReportsThatDayAndThatItIsNotToday()
    {
        var context = new TodayContext();

        var summary = await context.GetTodayAsync(LocalDate.AddDays(-1));

        Assert.Equal(LocalDate.AddDays(-1), summary.LocalDate);
        Assert.False(summary.IsToday);
    }

    [Fact]
    public async Task ItemsFromAnotherDayAreNotCounted()
    {
        var context = new TodayContext();
        await context.AddPlanningItemAsync("Today", LocalDate);
        await context.AddPlanningItemAsync("Tomorrow", LocalDate.AddDays(1));

        var summary = await context.GetTodayAsync();

        Assert.Single(summary.Occurrences);
        Assert.Equal("Today", summary.Occurrences[0].Title);
        Assert.Equal(1, summary.Progress.PlannedItemCount);
    }

    [Fact]
    public async Task CompletedAndPlannedItemsAreCountedSeparately()
    {
        var context = new TodayContext();
        var first = await context.AddPlanningItemAsync("Wake up", LocalDate);
        await context.AddPlanningItemAsync("Train", LocalDate);
        await context.AddPlanningItemAsync("Study", LocalDate);
        await context.SetStatusAsync(first.Id, LocalDate, OccurrenceStatus.Completed);

        var summary = await context.GetTodayAsync();

        Assert.Equal(3, summary.Progress.PlannedItemCount);
        Assert.Equal(1, summary.Progress.CompletedItemCount);
    }

    [Fact]
    public async Task ACancelledItemDoesNotInflateThePlannedCount()
    {
        var context = new TodayContext();
        var item = await context.AddPlanningItemAsync("Dropped", LocalDate);
        await context.SetStatusAsync(item.Id, LocalDate, OccurrenceStatus.Cancelled);

        var summary = await context.GetTodayAsync();

        Assert.Equal(0, summary.Progress.PlannedItemCount);
    }

    [Fact]
    public async Task RoutinesThatApplyToTheDayAppearWithTheirExecutionState()
    {
        var context = new TodayContext();
        var routine = await context.AddDailyRoutineAsync("Morning routine");
        var session = await context.RoutineService.StartSessionAsync(
            UserA,
            routine.Id,
            LocalDate,
            CancellationToken.None);
        await context.RoutineService.SaveSessionAsync(
            UserA,
            session.Value!.Id,
            new RoutineSessionInput(null, IsCompleted: true, []),
            CancellationToken.None);

        var summary = await context.GetTodayAsync();

        Assert.Single(summary.Routines);
        Assert.True(summary.Routines[0].IsCompleted);
        Assert.Equal(1, summary.Progress.RoutineCount);
        Assert.Equal(1, summary.Progress.CompletedRoutineCount);
    }

    [Fact]
    public async Task ARoutineThatDoesNotFallOnTheDayIsAbsent()
    {
        var context = new TodayContext();
        await context.AddWeeklyRoutineAsync("Monday workout", LocalDate.AddDays(-3));

        // The routine started on a Monday, so it does not fall on this Thursday.
        var summary = await context.GetTodayAsync();

        Assert.Empty(summary.Routines);
    }

    [Fact]
    public async Task CaloriesAndTheTargetAreReportedTogether()
    {
        var context = new TodayContext();
        await context.NutritionService.SaveGoalAsync(
            UserA,
            new NutritionGoalInput(2000, null, null, null),
            CancellationToken.None);
        await context.AddMealAsync("Breakfast", 420);
        await context.AddMealAsync("Lunch", 700);

        var summary = await context.GetTodayAsync();

        Assert.Equal(1120, summary.Progress.ConsumedCalories);
        Assert.Equal(2000, summary.Progress.DailyCalorieTarget);
        Assert.Equal(880, summary.Nutrition.RemainingCalories);
    }

    [Fact]
    public async Task StudyMinutesAreSummedForTheDay()
    {
        var context = new TodayContext();
        var project = await context.AddStudyProjectAsync("Angular");
        await context.AddStudySessionAsync(project.Id, LocalDate, 45);
        await context.AddStudySessionAsync(project.Id, LocalDate, 30);
        await context.AddStudySessionAsync(project.Id, LocalDate.AddDays(-1), 90);

        var summary = await context.GetTodayAsync();

        Assert.Equal(75, summary.Progress.StudyMinutes);
        Assert.Equal(2, summary.StudySessions.Count);
    }

    [Fact]
    public async Task TheJournalIsReportedAsAFlagAndNeverAsText()
    {
        var context = new TodayContext();
        await context.JournalService.SaveAsync(
            UserA,
            LocalDate,
            new JournalEntryInput("Something private.", null, null, null, null, null),
            CancellationToken.None);

        var summary = await context.GetTodayAsync();

        Assert.True(summary.Progress.JournalCompleted);

        // The reflection itself must not be reachable through the Today summary at all.
        var serialized = System.Text.Json.JsonSerializer.Serialize(summary);
        Assert.DoesNotContain("Something private", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMixedDayReportsEveryModuleAtOnce()
    {
        var context = new TodayContext();
        var done = await context.AddPlanningItemAsync("Wake up", LocalDate);
        await context.AddPlanningItemAsync("Train", LocalDate);
        await context.SetStatusAsync(done.Id, LocalDate, OccurrenceStatus.Completed);
        await context.AddDailyRoutineAsync("Morning routine");
        await context.AddMealAsync("Breakfast", 420);
        var project = await context.AddStudyProjectAsync("Angular");
        await context.AddStudySessionAsync(project.Id, LocalDate, 45);

        var summary = await context.GetTodayAsync();

        Assert.Equal(2, summary.Occurrences.Count);
        Assert.Single(summary.Routines);
        Assert.Single(summary.Nutrition.Meals);
        Assert.Single(summary.StudySessions);
        Assert.Equal(1, summary.Progress.CompletedItemCount);
        Assert.Equal(420, summary.Progress.ConsumedCalories);
        Assert.Equal(45, summary.Progress.StudyMinutes);
    }

    /// <summary>
    /// Wires the real services onto in-memory stores, a fixed clock, and a fixed time zone.
    /// </summary>
    private sealed class TodayContext
    {
        public TodayContext(DateTimeOffset? utcNow = null, string timeZoneId = "America/Costa_Rica")
        {
            UtcNow = utcNow ?? new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);
            var clock = new FixedClock(UtcNow);
            var profileStore = new FixedTimeZoneProfileStore(timeZoneId);

            CalendarStore = new InMemoryCalendarStore();
            CalendarService = new CalendarService(
                CalendarStore,
                new TimeContextService(clock, profileStore, new LocalTimeService()),
                clock);
            RoutineService = new RoutineService(new InMemoryRoutineStore(), clock);
            NutritionService = new NutritionService(new InMemoryNutritionStore(), clock);
            StudyService = new StudyService(new InMemoryStudyStore(), clock);
            JournalService = new JournalService(new InMemoryJournalStore(), clock);

            Service = new TodayService(
                new TimeContextService(clock, profileStore, new LocalTimeService()),
                CalendarService,
                RoutineService,
                NutritionService,
                StudyService,
                JournalService);
        }

        public DateTimeOffset UtcNow { get; }

        public InMemoryCalendarStore CalendarStore { get; }

        public CalendarService CalendarService { get; }

        public RoutineService RoutineService { get; }

        public NutritionService NutritionService { get; }

        public StudyService StudyService { get; }

        public JournalService JournalService { get; }

        private TodayService Service { get; }

        public Task<TodaySummaryRecord> GetTodayAsync(DateOnly? date = null) =>
            Service.GetAsync(UserA, date, CancellationToken.None);

        public async Task<PlanningItemRecord> AddPlanningItemAsync(string title, DateOnly date)
        {
            var result = await CalendarService.CreateAsync(
                UserA,
                new SavePlanningItemInput(
                    title,
                    null,
                    PlanningItemKind.Task,
                    PlanningCategory.General,
                    PlanningPriority.Normal,
                    date,
                    null,
                    null,
                    null),
                CancellationToken.None);

            return result.Value!;
        }

        public Task SetStatusAsync(Guid itemId, DateOnly date, OccurrenceStatus status) =>
            CalendarService.SetOccurrenceStatusAsync(
                UserA,
                itemId,
                date,
                status,
                CancellationToken.None);

        public async Task<RoutineTemplateRecord> AddDailyRoutineAsync(string name)
        {
            var result = await RoutineService.CreateAsync(
                UserA,
                new RoutineTemplateInput(
                    name,
                    null,
                    RoutineCategory.General,
                    new RecurrenceInput(RecurrenceFrequency.Daily, 1, LocalDate, null, []),
                    IsActive: true,
                    []),
                CancellationToken.None);

            return result.Value!;
        }

        public async Task<RoutineTemplateRecord> AddWeeklyRoutineAsync(string name, DateOnly start)
        {
            var result = await RoutineService.CreateAsync(
                UserA,
                new RoutineTemplateInput(
                    name,
                    null,
                    RoutineCategory.Workout,
                    new RecurrenceInput(RecurrenceFrequency.Weekly, 1, start, null, []),
                    IsActive: true,
                    []),
                CancellationToken.None);

            return result.Value!;
        }

        public Task AddMealAsync(string name, int calories) =>
            NutritionService.CreateMealAsync(
                UserA,
                new MealEntryInput(
                    LocalDate,
                    MealType.Breakfast,
                    name,
                    null,
                    calories,
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None);

        public async Task<StudyProjectRecord> AddStudyProjectAsync(string name)
        {
            var result = await StudyService.CreateProjectAsync(
                UserA,
                new StudyProjectInput(name, null, StudyProjectStatus.Active, []),
                CancellationToken.None);

            return result.Value!;
        }

        public Task AddStudySessionAsync(Guid projectId, DateOnly date, int minutes) =>
            StudyService.CreateSessionAsync(
                UserA,
                new StudySessionInput(projectId, date, null, minutes, null, null),
                CancellationToken.None);
    }
}
