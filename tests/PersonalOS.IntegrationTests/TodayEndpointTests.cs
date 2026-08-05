using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The integrated Today endpoint.
/// </summary>
public sealed class TodayEndpointTests
{
    /// <summary>Thursday 30 July 2026, the instant the test host clock reports.</summary>
    private const string LocalDate = "2026-07-30";

    [Fact]
    public async Task TodayRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);

        var response = await client.GetAsync("/api/today");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnEmptyDayReportsZeroesRatherThanInventedNumbers()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        Assert.Empty(summary!.Occurrences);
        Assert.Empty(summary.Routines);
        Assert.Empty(summary.StudySessions);
        Assert.Empty(summary.Nutrition.Meals);
        Assert.Equal(0, summary.Progress.PlannedItemCount);
        Assert.Null(summary.Progress.DailyCalorieTarget);
        Assert.False(summary.Progress.JournalCompleted);
    }

    [Fact]
    public async Task TheLocalDayComesFromTheSavedTimeZoneAndTheServerClock()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        // 00:30 UTC on 31 July is still 30 July in Costa Rica.
        factory.UtcNow = new DateTimeOffset(2026, 7, 31, 0, 30, 0, TimeSpan.Zero);
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/profile", new
        {
            displayName = "Jefferson",
            timeZoneId = "America/Costa_Rica",
        });

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        Assert.Equal(new DateOnly(2026, 7, 30), summary!.LocalDate);
        Assert.Equal("America/Costa_Rica", summary.TimeZoneId);
        Assert.True(summary.IsToday);
        Assert.Equal(new TimeOnly(18, 30), summary.LocalTimeOfDay);
    }

    [Fact]
    public async Task TheSameInstantIsAlreadyTheNextDayFurtherEast()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        factory.UtcNow = new DateTimeOffset(2026, 7, 30, 23, 30, 0, TimeSpan.Zero);
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/profile", new
        {
            displayName = "Jefferson",
            timeZoneId = "Asia/Tokyo",
        });

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        Assert.Equal(new DateOnly(2026, 7, 31), summary!.LocalDate);
    }

    [Fact]
    public async Task AskingForAnotherDayReportsThatDayAndThatItIsNotToday()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today?date=2026-07-29");

        Assert.Equal(new DateOnly(2026, 7, 29), summary!.LocalDate);
        Assert.False(summary.IsToday);
    }

    [Fact]
    public async Task TodayReflectsEveryModuleThatHasPersistedData()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        // A planned item, one of them completed.
        var done = await CreateItemAsync(client, "Wake up");
        await CreateItemAsync(client, "Train");
        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/calendar/items/{done}/occurrences/{LocalDate}/status",
            new { status = "completed" });

        // A routine that falls on this Thursday.
        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", new
        {
            name = "Morning routine",
            category = "general",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new { frequency = "daily", interval = 1, startDate = LocalDate },
        });

        // A calorie target and a meal.
        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/nutrition/goal", new
        {
            dailyCalorieTarget = 2000,
        });
        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/meals", new
        {
            localDate = LocalDate,
            mealType = "breakfast",
            name = "Oats",
            calories = 420,
        });

        // A study session.
        var projectResponse = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/study/projects",
            new { name = "Angular", status = "active", resources = Array.Empty<object>() });
        var project = await DailyApi.ReadAsync<StudyProjectDto>(projectResponse);
        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/study/sessions", new
        {
            studyProjectId = project!.Id,
            localDate = LocalDate,
            durationMinutes = 45,
        });

        // A reflection.
        await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/journal/{LocalDate}", new
        {
            wentWell = "Kept the plan.",
        });

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        Assert.Equal(2, summary!.Occurrences.Count);
        Assert.Single(summary.Routines);
        Assert.Single(summary.Nutrition.Meals);
        Assert.Single(summary.StudySessions);
        Assert.Equal(2, summary.Progress.PlannedItemCount);
        Assert.Equal(1, summary.Progress.CompletedItemCount);
        Assert.Equal(1, summary.Progress.RoutineCount);
        Assert.Equal(0, summary.Progress.CompletedRoutineCount);
        Assert.Equal(420, summary.Progress.ConsumedCalories);
        Assert.Equal(2000, summary.Progress.DailyCalorieTarget);
        Assert.Equal(45, summary.Progress.StudyMinutes);
        Assert.True(summary.Progress.JournalCompleted);
        Assert.Equal(1580, summary.Nutrition.RemainingCalories);
    }

    [Fact]
    public async Task CompletingARoutineIsVisibleOnTodayImmediately()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var routineResponse = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", new
        {
            name = "Morning routine",
            category = "general",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new { frequency = "daily", interval = 1, startDate = LocalDate },
        });
        var routine = await DailyApi.ReadAsync<RoutineDto>(routineResponse);

        var sessionResponse = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            $"/api/routines/{routine!.Id}/sessions",
            new { localDate = LocalDate });
        var session = await DailyApi.ReadAsync<RoutineSessionDto>(sessionResponse);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/routine-sessions/{session!.Id}",
            new { isCompleted = true, stepResults = Array.Empty<object>() });

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        Assert.Equal(1, summary!.Progress.CompletedRoutineCount);
        Assert.True(summary.Routines[0].IsCompleted);
    }

    [Fact]
    public async Task TwoAccountsSeeOnlyTheirOwnDay()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await CreateItemAsync(clientA, "Account A item");
        await DailyApi.SendAsync(clientA, HttpMethod.Post, "/api/meals", new
        {
            localDate = LocalDate,
            mealType = "breakfast",
            name = "Oats",
            calories = 420,
        });

        var summaryB = await DailyApi.GetAsync<TodaySummaryDto>(clientB, "/api/today");

        Assert.Empty(summaryB!.Occurrences);
        Assert.Equal(0, summaryB.Progress.ConsumedCalories);
        Assert.Equal(0, summaryB.Progress.PlannedItemCount);
    }

    [Fact]
    public async Task TodayUsesNoStoreCachingAndExposesNoAccountIdentifier()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await CreateItemAsync(client, "Train");

        var response = await client.GetAsync("/api/today");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a calendar item through the calendar endpoints.
    /// </summary>
    /// <remarks>
    /// Today has no create endpoint of its own. It reads the calendar's projection, which is what
    /// keeps planning a day and working through it from drifting apart.
    /// </remarks>
    private static async Task<Guid> CreateItemAsync(HttpClient client, string title)
    {
        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title,
            kind = "task",
            category = "general",
            priority = "normal",
            startDate = LocalDate,
        });
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<PlanningItemDto>(response))!.Id;
    }
}
