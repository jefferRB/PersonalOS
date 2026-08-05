using PersonalOS.Application.Common;
using PersonalOS.Application.Journal;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Study;
using PersonalOS.Domain.Journal;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Domain.Study;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Daily;

/// <summary>
/// Nutrition arithmetic and its technical boundaries.
/// </summary>
public sealed class NutritionServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");
    private static readonly DateOnly LocalDate = new(2026, 7, 30);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    private readonly InMemoryNutritionStore store = new();
    private readonly NutritionService service;

    public NutritionServiceTests()
    {
        service = new NutritionService(store, new FixedClock(UtcNow));
    }

    [Fact]
    public async Task AnAccountWithNoGoalReportsNoTargetRatherThanAGuess()
    {
        var goal = await service.GetGoalAsync(UserA, CancellationToken.None);
        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Null(goal.DailyCalorieTarget);
        Assert.Null(day.RemainingCalories);
    }

    [Fact]
    public async Task CaloriesAreSummedAcrossTheMealsOfADay()
    {
        await AddMealAsync("Breakfast", 420);
        await AddMealAsync("Lunch", 700);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(1120, day.ConsumedCalories);
        Assert.Equal(2, day.Meals.Count);
    }

    [Fact]
    public async Task OptionalMacrosAreSummedAndMissingOnesCountAsZero()
    {
        await AddMealAsync("Oats", 420, protein: 12m);
        await AddMealAsync("Water", 0);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(12m, day.ProteinGrams);
        Assert.Equal(0m, day.CarbohydrateGrams);
    }

    [Fact]
    public async Task RemainingCaloriesIsTheTargetMinusWhatWasEaten()
    {
        await SaveGoalAsync(2000);
        await AddMealAsync("Breakfast", 420);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(1580, day.RemainingCalories);
    }

    [Fact]
    public async Task GoingOverTheTargetReportsANegativeNumberRatherThanAnError()
    {
        await SaveGoalAsync(2000);
        await AddMealAsync("Large dinner", 2400);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        // The value is a fact. Nothing here judges it, and nothing recommends a different number.
        Assert.Equal(-400, day.RemainingCalories);
        Assert.Equal(2400, day.ConsumedCalories);
    }

    [Theory]
    [InlineData(499)]
    [InlineData(20001)]
    public async Task ACalorieTargetOutsideTheStoredRangeIsRejected(int target)
    {
        var result = await service.SaveGoalAsync(
            UserA,
            new NutritionGoalInput(target, null, null, null),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(NutritionService.DailyCalorieTargetField));
    }

    [Fact]
    public async Task ANegativeCalorieValueIsRejected()
    {
        var result = await service.CreateMealAsync(
            UserA,
            Meal("Oats", -1),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(NutritionService.CaloriesField));
    }

    [Fact]
    public async Task ANegativeMacroValueIsRejected()
    {
        var result = await service.CreateMealAsync(
            UserA,
            Meal("Oats", 420) with { ProteinGrams = -5m },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(NutritionService.ProteinField));
    }

    [Fact]
    public async Task AMealWithoutANameIsRejected()
    {
        var result = await service.CreateMealAsync(
            UserA,
            Meal("   ", 420),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(NutritionService.NameField));
    }

    [Fact]
    public async Task SavingTheGoalTwiceReplacesItRatherThanAccumulating()
    {
        await SaveGoalAsync(2000);
        await SaveGoalAsync(2200);

        var goal = await service.GetGoalAsync(UserA, CancellationToken.None);

        Assert.Equal(2200, goal.DailyCalorieTarget);
    }

    [Fact]
    public async Task TwoAccountsKeepIndependentGoalsAndMeals()
    {
        await SaveGoalAsync(2000);
        await AddMealAsync("Breakfast", 420);

        var dayB = await service.GetDayAsync(UserB, LocalDate, CancellationToken.None);

        Assert.Equal(0, dayB.ConsumedCalories);
        Assert.Null(dayB.Goal.DailyCalorieTarget);
    }

    [Fact]
    public async Task OneAccountCannotEditOrDeleteAnotherAccountsMeal()
    {
        var created = await service.CreateMealAsync(UserA, Meal("Oats", 420), CancellationToken.None);

        var update = await service.UpdateMealAsync(
            UserB,
            created.Value!.Id,
            Meal("Hijacked", 1),
            CancellationToken.None);
        var deleted = await service.DeleteMealAsync(UserB, created.Value.Id, CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, update.Status);
        Assert.False(deleted);
    }

    private Task SaveGoalAsync(int target) =>
        service.SaveGoalAsync(
            UserA,
            new NutritionGoalInput(target, null, null, null),
            CancellationToken.None);

    private Task AddMealAsync(string name, int calories, decimal? protein = null) =>
        service.CreateMealAsync(
            UserA,
            Meal(name, calories) with { ProteinGrams = protein },
            CancellationToken.None);

    private static MealEntryInput Meal(string name, int calories) =>
        new(LocalDate, MealType.Breakfast, name, null, calories, null, null, null, null, null);
}

/// <summary>
/// Study projects, resource links, and weekly aggregation.
/// </summary>
public sealed class StudyServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");
    private static readonly DateOnly Monday = new(2026, 7, 27);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 27, 13, 0, 0, TimeSpan.Zero);

    private readonly InMemoryStudyStore store = new();
    private readonly StudyService service;

    public StudyServiceTests()
    {
        service = new StudyService(store, new FixedClock(UtcNow));
    }

    [Fact]
    public async Task AProjectStoresItsResourceMetadata()
    {
        var result = await service.CreateProjectAsync(
            UserA,
            Project() with
            {
                Resources =
                [
                    new StudyResourceInput(
                        "Signals guide",
                        StudyResourceType.Article,
                        "https://angular.dev/guide/signals",
                        null),
                ],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://angular.dev/guide/signals", result.Value!.Resources[0].ExternalUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("angular.dev")]
    public async Task AResourceLinkThatIsNotHttpOrHttpsIsRejected(string url)
    {
        // Rejecting the scheme at the point of storage means no template can ever render it.
        Assert.False(ExternalUrlRules.IsAcceptable(url));

        var result = await service.CreateProjectAsync(
            UserA,
            Project() with
            {
                Resources = [new StudyResourceInput("Notes", StudyResourceType.Other, url, null)],
            },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(StudyService.ResourcesField));
    }

    [Fact]
    public async Task AResourceWithNoLinkIsAccepted()
    {
        var result = await service.CreateProjectAsync(
            UserA,
            Project() with
            {
                Resources = [new StudyResourceInput("Paper notebook", StudyResourceType.Other, null, null)],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Resources[0].ExternalUrl);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(1441)]
    public async Task AnUnusableDurationIsRejected(int minutes)
    {
        var project = await service.CreateProjectAsync(UserA, Project(), CancellationToken.None);

        var result = await service.CreateSessionAsync(
            UserA,
            Session(project.Value!.Id, Monday, minutes),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(StudyService.DurationField));
    }

    [Fact]
    public async Task SessionsAggregateAcrossAWeek()
    {
        var project = await service.CreateProjectAsync(UserA, Project(), CancellationToken.None);
        await service.CreateSessionAsync(
            UserA,
            Session(project.Value!.Id, Monday, 45),
            CancellationToken.None);
        await service.CreateSessionAsync(
            UserA,
            Session(project.Value.Id, Monday.AddDays(3), 90),
            CancellationToken.None);
        // Outside the week, so it must not be counted.
        await service.CreateSessionAsync(
            UserA,
            Session(project.Value.Id, Monday.AddDays(10), 60),
            CancellationToken.None);

        var week = await service.GetSessionsAsync(
            UserA,
            Monday,
            Monday.AddDays(6),
            CancellationToken.None);

        Assert.Equal(2, week.Value!.Count);
        Assert.Equal(135, week.Value.Sum(session => session.DurationMinutes));
    }

    [Fact]
    public async Task ASessionCarriesItsProjectNameSoTheClientNeedsNoSecondRequest()
    {
        var project = await service.CreateProjectAsync(UserA, Project(), CancellationToken.None);
        await service.CreateSessionAsync(
            UserA,
            Session(project.Value!.Id, Monday, 45),
            CancellationToken.None);

        var week = await service.GetSessionsAsync(
            UserA,
            Monday,
            Monday.AddDays(6),
            CancellationToken.None);

        Assert.Equal("Angular", week.Value![0].ProjectName);
    }

    [Fact]
    public async Task ASessionCannotBeAttachedToAnotherAccountsProject()
    {
        var project = await service.CreateProjectAsync(UserA, Project(), CancellationToken.None);

        var result = await service.CreateSessionAsync(
            UserB,
            Session(project.Value!.Id, Monday, 45),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(StudyService.StudyProjectIdField));
    }

    [Fact]
    public async Task OneAccountCannotSeeOrEditAnotherAccountsProject()
    {
        var project = await service.CreateProjectAsync(UserA, Project(), CancellationToken.None);

        var update = await service.UpdateProjectAsync(
            UserB,
            project.Value!.Id,
            Project(),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, update.Status);
        Assert.Empty(await service.GetProjectsAsync(UserB, CancellationToken.None));
    }

    private static StudyProjectInput Project() =>
        new("Angular", null, StudyProjectStatus.Active, []);

    private static StudySessionInput Session(Guid projectId, DateOnly date, int minutes) =>
        new(projectId, date, null, minutes, null, null);
}

/// <summary>
/// The daily reflection, including the one-entry-per-day rule.
/// </summary>
public sealed class JournalServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");
    private static readonly DateOnly LocalDate = new(2026, 7, 30);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 22, 0, 0, TimeSpan.Zero);

    private readonly InMemoryJournalStore store = new();
    private readonly JournalService service;

    public JournalServiceTests()
    {
        service = new JournalService(store, new FixedClock(UtcNow));
    }

    [Fact]
    public async Task ADayWithNoEntryReadsAsEmptyRatherThanAsAnError()
    {
        var entry = await service.GetAsync(UserA, LocalDate, CancellationToken.None);

        Assert.False(entry.HasContent);
        Assert.Null(entry.WentWell);
        Assert.Equal(LocalDate, entry.LocalDate);
    }

    [Fact]
    public async Task SavingTheSameDayTwiceUpdatesOneEntry()
    {
        await SaveAsync("First version");
        await SaveAsync("Second version");

        var entry = await service.GetAsync(UserA, LocalDate, CancellationToken.None);

        // One entry per account per day is the product rule and the database invariant.
        Assert.Equal(1, store.Count);
        Assert.Equal("Second version", entry.WentWell);
    }

    [Fact]
    public async Task TwoDifferentDaysProduceTwoEntries()
    {
        await SaveAsync("Thursday");
        await service.SaveAsync(
            UserA,
            LocalDate.AddDays(1),
            new JournalEntryInput("Friday", null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(2, store.Count);
    }

    [Fact]
    public async Task EverySectionIsOptionalSoOneSentenceIsACompleteEntry()
    {
        var result = await SaveAsync("A quiet day.");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasContent);
        Assert.Null(result.Value.Lesson);
    }

    [Fact]
    public async Task AnEntryWithOnlyWhitespaceHoldsNoContent()
    {
        var result = await service.SaveAsync(
            UserA,
            LocalDate,
            new JournalEntryInput("   ", "  ", null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasContent);
    }

    [Fact]
    public async Task ASectionLongerThanTheColumnIsRejectedWithoutEchoingTheText()
    {
        var secret = new string('x', DailyJournalEntry.SectionMaxLength + 1);

        var result = await service.SaveAsync(
            UserA,
            LocalDate,
            new JournalEntryInput(secret, null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);

        var message = result.ValidationErrors["wentWell"][0];

        // The message states the limit and never repeats what the user wrote.
        Assert.Contains(DailyJournalEntry.SectionMaxLength.ToString(), message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrittenDatesReportOnlyTheDayAndNeverTheText()
    {
        await SaveAsync("Something private.");

        var dates = await service.GetWrittenDatesAsync(
            UserA,
            LocalDate.AddDays(-3),
            LocalDate,
            CancellationToken.None);

        // Today needs to know that the day was reflected on, not what the reflection says.
        Assert.Equal([LocalDate], dates);
    }

    [Fact]
    public async Task AnEmptyEntryIsNotCountedAsWritten()
    {
        await service.SaveAsync(
            UserA,
            LocalDate,
            new JournalEntryInput(null, null, null, null, null, null),
            CancellationToken.None);

        var dates = await service.GetWrittenDatesAsync(
            UserA,
            LocalDate,
            LocalDate,
            CancellationToken.None);

        Assert.Empty(dates);
    }

    [Fact]
    public async Task OneAccountCannotReadAnotherAccountsReflection()
    {
        await SaveAsync("Something private.");

        var entry = await service.GetAsync(UserB, LocalDate, CancellationToken.None);
        var dates = await service.GetWrittenDatesAsync(
            UserB,
            LocalDate,
            LocalDate,
            CancellationToken.None);

        Assert.False(entry.HasContent);
        Assert.Null(entry.WentWell);
        Assert.Empty(dates);
    }

    [Fact]
    public async Task SavingRecordsTheInstantFromTheApplicationClock()
    {
        var result = await SaveAsync("Recorded.");

        Assert.Equal(UtcNow, result.Value!.UpdatedAtUtc);
    }

    private Task<OperationResult<JournalEntryRecord>> SaveAsync(string wentWell) =>
        service.SaveAsync(
            UserA,
            LocalDate,
            new JournalEntryInput(wentWell, null, null, null, null, null),
            CancellationToken.None);
}
