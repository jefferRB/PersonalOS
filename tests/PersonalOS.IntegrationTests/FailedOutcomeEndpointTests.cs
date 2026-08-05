using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// Recording a failed outcome through the real HTTP pipeline.
/// </summary>
/// <remarks>
/// The outcome goes through the existing occurrence-status endpoint rather than one of its own: it
/// is another answer to the same question, not a different operation.
/// </remarks>
public sealed class FailedOutcomeEndpointTests
{
    /// <summary>Thursday 30 July 2026, the instant the test host clock reports.</summary>
    private const string LocalDate = "2026-07-30";

    [Fact]
    public async Task TodayCanBeMarkedFailed()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate);

        var response = await SetStatusAsync(client, id, LocalDate, "failed");
        var occurrence = await DailyApi.ReadAsync<CalendarOccurrenceDto>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("failed", occurrence!.Status);
        Assert.Null(occurrence.CompletedAtUtc);
    }

    [Fact]
    public async Task APastOccurrenceCanBeMarkedFailed()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, "2026-07-27");

        var response = await SetStatusAsync(client, id, "2026-07-27", "failed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AFutureOccurrenceIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, "2026-08-05");

        var response = await SetStatusAsync(client, id, "2026-08-05", "failed");
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("occurrenceDate"));
    }

    [Fact]
    public async Task TheBoundaryFollowsTheAccountsSavedTimeZone()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        // 03:00 UTC on 31 July is still 30 July in Costa Rica and already the 31st in Tokyo.
        factory.UtcNow = new DateTimeOffset(2026, 7, 31, 3, 0, 0, TimeSpan.Zero);

        using var westward = await DailyApi.SignInAsync(factory, "Costa Rica");
        using var eastward = await DailyApi.SignInAsync(factory, "Tokyo");

        await DailyApi.SendAsync(westward, HttpMethod.Put, "/api/profile", new
        {
            displayName = "Costa Rica",
            timeZoneId = "America/Costa_Rica",
        });
        await DailyApi.SendAsync(eastward, HttpMethod.Put, "/api/profile", new
        {
            displayName = "Tokyo",
            timeZoneId = "Asia/Tokyo",
        });

        var westwardId = await CreateItemAsync(westward, "2026-07-31");
        var eastwardId = await CreateItemAsync(eastward, "2026-07-31");

        var westwardResponse = await SetStatusAsync(westward, westwardId, "2026-07-31", "failed");
        var eastwardResponse = await SetStatusAsync(eastward, eastwardId, "2026-07-31", "failed");

        Assert.Equal(HttpStatusCode.BadRequest, westwardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, eastwardResponse.StatusCode);
    }

    [Fact]
    public async Task MarkingFailedTwiceIsIdempotent()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate);

        await SetStatusAsync(client, id, LocalDate, "failed");
        var second = await SetStatusAsync(client, id, LocalDate, "failed");
        var day = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            $"/api/calendar/day?date={LocalDate}");

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("failed", day!.Occurrences.Single().Status);
    }

    [Fact]
    public async Task AFailedOccurrenceCanBeReopened()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate);

        await SetStatusAsync(client, id, LocalDate, "failed");
        var reopened = await SetStatusAsync(client, id, LocalDate, "planned");
        var occurrence = await DailyApi.ReadAsync<CalendarOccurrenceDto>(reopened);

        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
        Assert.Equal("planned", occurrence!.Status);
    }

    [Fact]
    public async Task AMonthSummaryReportsFailedCountsWithoutPrivateText()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate, "Therapy session");

        await SetStatusAsync(client, id, LocalDate, "failed");

        var response = await client.GetAsync("/api/calendar/month?year=2026&month=7");
        var body = await response.Content.ReadAsStringAsync();
        var month = await DailyApi.ReadAsync<CalendarMonthDto>(response);
        var day = month!.Days.Single(summary => summary.Date == new DateOnly(2026, 7, 30));

        Assert.Equal(1, day.FailedCount);
        Assert.Equal(0, day.CancelledCount);
        Assert.DoesNotContain("Therapy session", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnotherAccountCannotMarkAnOccurrenceFailed()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var id = await CreateItemAsync(clientA, LocalDate);

        var response = await SetStatusAsync(clientB, id, LocalDate, "failed");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordingAFailedOutcomeRequiresAValidAntiforgeryToken()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate);
        var url = $"/api/calendar/items/{id}/occurrences/{LocalDate}/status";

        var missing = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Put,
            url,
            new { status = "failed" });
        var invalid = await DailyApi.SendWithInvalidAntiforgeryAsync(
            client,
            HttpMethod.Put,
            url,
            new { status = "failed" });
        var valid = await DailyApi.SendAsync(client, HttpMethod.Put, url, new { status = "failed" });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task TodayReportsAFailedOccurrenceThroughTheSharedProjection()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client, LocalDate);

        await SetStatusAsync(client, id, LocalDate, "failed");

        var summary = await DailyApi.GetAsync<TodaySummaryDto>(client, "/api/today");

        // Today reads the calendar's projection, so a failed day needs no model of its own there.
        Assert.Equal("failed", summary!.Occurrences.Single().Status);
        Assert.Equal(1, summary.Progress.PlannedItemCount);
        Assert.Equal(0, summary.Progress.CompletedItemCount);
    }

    private static async Task<Guid> CreateItemAsync(
        HttpClient client,
        string startDate,
        string title = "Run")
    {
        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title,
            kind = "task",
            category = "fitness",
            priority = "normal",
            startDate,
        });
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<PlanningItemDto>(response))!.Id;
    }

    private static Task<HttpResponseMessage> SetStatusAsync(
        HttpClient client,
        Guid itemId,
        string occurrenceDate,
        string status) =>
        DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/calendar/items/{itemId}/occurrences/{occurrenceDate}/status",
            new { status });
}
