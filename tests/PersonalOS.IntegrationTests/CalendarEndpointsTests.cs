using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The calendar endpoints, exercised through the real HTTP pipeline.
/// </summary>
/// <remarks>
/// Every request here goes through authentication, antiforgery, model binding, validation, and
/// serialization exactly as a browser would drive them, so these tests cover the parts a unit test
/// on the service cannot reach.
/// </remarks>
public sealed class CalendarEndpointsTests
{
    /// <summary>Thursday 30 July 2026, the instant the test host clock reports.</summary>
    private const string LocalDate = "2026-07-30";

    [Fact]
    public async Task EveryCalendarRouteRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync("/api/calendar/month?year=2026&month=7"),
            await client.GetAsync($"/api/calendar/day?date={LocalDate}"),
            await client.GetAsync($"/api/calendar/upcoming?from={LocalDate}"),
            await client.GetAsync($"/api/calendar/items/{id}"),
            await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", NewItem()),
            await DailyApi.SendAsync(
                client,
                HttpMethod.Put,
                $"/api/calendar/items/{id}",
                NewItem()),
            await DailyApi.SendAsync(client, HttpMethod.Delete, $"/api/calendar/items/{id}"),
            await DailyApi.SendAsync(
                client,
                HttpMethod.Put,
                $"/api/calendar/items/{id}/occurrences/{LocalDate}/status",
                new { status = "completed" }),
        };

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.All(responses, response =>
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType));
    }

    [Fact]
    public async Task AnUnauthorizedResponseIsNeverAnHtmlRedirect()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);

        var response = await client.GetAsync("/api/calendar/month?year=2026&month=7");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatingAnItemReturnsItAndItCanBeReadBack()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/calendar/items",
            NewItem());
        var created = await DailyApi.ReadAsync<PlanningItemDto>(response);

        var read = await DailyApi.GetAsync<PlanningItemDto>(
            client,
            $"/api/calendar/items/{created!.Id}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Dentist", created.Title);
        Assert.Equal("appointment", created.Kind);
        Assert.Equal("none", created.Recurrence.Frequency);
        Assert.Equal(created.Id, read!.Id);
    }

    [Fact]
    public async Task ADayListsTheOccurrencesOfThatDay()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await CreateItemAsync(client);

        var day = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            $"/api/calendar/day?date={LocalDate}");

        Assert.Equal(new DateOnly(2026, 7, 30), day!.Date);
        Assert.Equal("Dentist", day.Occurrences.Single().Title);
        Assert.Equal("planned", day.Occurrences.Single().Status);
    }

    [Fact]
    public async Task ADayWithNoDateUsesTheAccountsSavedTimeZone()
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

        var day = await DailyApi.GetAsync<CalendarDayDto>(client, "/api/calendar/day");

        Assert.Equal(new DateOnly(2026, 7, 30), day!.Date);
        Assert.Equal(new DateOnly(2026, 7, 30), day.TodayLocalDate);
        Assert.Equal("America/Costa_Rica", day.TimeZoneId);
        Assert.Equal(new TimeOnly(18, 30), day.LocalTimeOfDay);
    }

    [Fact]
    public async Task AMonthCountsGeneratedOccurrencesWithoutStoringThem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "Stretch",
            kind = "routine",
            category = "fitness",
            priority = "normal",
            startDate = "2026-07-01",
            recurrence = new { frequency = "daily", interval = 1, endDate = "2026-07-31" },
        });

        var month = await DailyApi.GetAsync<CalendarMonthDto>(
            client,
            "/api/calendar/month?year=2026&month=7");

        var july = month!.Days.Where(day => day.Date.Month == 7).ToList();

        Assert.Equal(31, july.Count);
        Assert.All(july, day => Assert.Equal(1, day.TotalCount));
        Assert.All(july, day => Assert.Equal(["routine"], day.Kinds.Select(kind => kind.Kind)));
        Assert.All(july, day => Assert.Equal([1], day.Kinds.Select(kind => kind.Count)));
    }

    [Fact]
    public async Task AMonthResponseCarriesNoTitleOrDescription()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "Therapy session",
            description = "Somewhere private",
            kind = "appointment",
            category = "health",
            priority = "normal",
            startDate = LocalDate,
        });

        var response = await client.GetAsync("/api/calendar/month?year=2026&month=7");
        var body = await response.Content.ReadAsStringAsync();

        // A month view shows counts and kinds. Shipping the titles behind them would put a grid's
        // worth of private text on the wire that nothing on screen displays.
        Assert.DoesNotContain("Therapy session", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Somewhere private", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMonthOutsideTheCalendarIsRejectedWithProblemDetails()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await client.GetAsync("/api/calendar/month?year=2026&month=13");
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("month"));
    }

    [Fact]
    public async Task TheUpcomingWindowReturnsEverythingAndFlagsWhatIsImportant()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await CreateItemAsync(client, title: "Dentist", kind: "appointment");
        await CreateItemAsync(client, title: "Concert", kind: "event");
        await CreateItemAsync(client, title: "Ordinary task", kind: "task");
        await CreateItemAsync(client, title: "Urgent task", kind: "task", priority: "high");
        await CreateItemAsync(client, title: "Ordinary routine", kind: "routine");

        var week = await DailyApi.GetAsync<UpcomingWeekDto>(client, "/api/calendar/upcoming");
        var occurrences = week!.Days.Single().Occurrences;

        // Everything in the bounded window arrives, so the section's filters can run on the client
        // without a request per click.
        Assert.Equal(5, occurrences.Count);

        var important = occurrences
            .Where(occurrence => occurrence.IsImportant)
            .Select(occurrence => occurrence.Title)
            .Order()
            .ToList();

        // Events and appointments always count; a task or a routine only when marked high priority.
        Assert.Equal(["Concert", "Dentist", "Urgent task"], important);
        Assert.Equal(new DateOnly(2026, 7, 30), week.FromDate);
        Assert.Equal(new DateOnly(2026, 8, 5), week.ToDate);
    }

    [Fact]
    public async Task CompletingAnOccurrencePersistsAcrossRequests()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client);

        var response = await SetStatusAsync(client, id, LocalDate, "completed");
        var day = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            $"/api/calendar/day?date={LocalDate}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("completed", day!.Occurrences.Single().Status);
        Assert.NotNull(day.Occurrences.Single().CompletedAtUtc);
    }

    [Fact]
    public async Task CompletingTheSameOccurrenceTwiceIsIdempotent()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client);

        var first = await SetStatusAsync(client, id, LocalDate, "completed");
        var firstBody = await DailyApi.ReadAsync<CalendarOccurrenceDto>(first);

        var second = await SetStatusAsync(client, id, LocalDate, "completed");
        var secondBody = await DailyApi.ReadAsync<CalendarOccurrenceDto>(second);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstBody!.CompletedAtUtc, secondBody!.CompletedAtUtc);
    }

    [Fact]
    public async Task OnlyTheActedOnDayOfASeriesChanges()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "Stretch",
            kind = "routine",
            category = "fitness",
            priority = "normal",
            startDate = LocalDate,
            recurrence = new { frequency = "daily", interval = 1 },
        });
        var created = await DailyApi.ReadAsync<PlanningItemDto>(response);

        await SetStatusAsync(client, created!.Id, LocalDate, "completed");

        var completedDay = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            $"/api/calendar/day?date={LocalDate}");
        var nextDay = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            "/api/calendar/day?date=2026-07-31");

        Assert.Equal("completed", completedDay!.Occurrences.Single().Status);
        Assert.Equal("planned", nextDay!.Occurrences.Single().Status);
    }

    [Fact]
    public async Task ADayTheRuleDoesNotProduceIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client);

        var response = await SetStatusAsync(client, id, "2026-08-15", "completed");
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("occurrenceDate"));
    }

    [Fact]
    public async Task TheRepetitionIsFrozenOnceADayHasBeenActedOn()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "Stretch",
            kind = "routine",
            category = "fitness",
            priority = "normal",
            startDate = LocalDate,
            recurrence = new { frequency = "daily", interval = 1 },
        });
        var created = await DailyApi.ReadAsync<PlanningItemDto>(response);

        await SetStatusAsync(client, created!.Id, LocalDate, "completed");

        var update = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/calendar/items/{created.Id}",
            new
            {
                title = "Stretch",
                kind = "routine",
                category = "fitness",
                priority = "normal",
                startDate = LocalDate,
                recurrence = new { frequency = "weekly", interval = 1 },
            });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(update);

        var read = await DailyApi.GetAsync<PlanningItemDto>(
            client,
            $"/api/calendar/items/{created.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("recurrence"));
        // The editor is told before the user tries, so the refusal above is a backstop rather than
        // the normal way this rule is discovered.
        Assert.True(read!.IsRecurrencePatternLocked);
    }

    [Fact]
    public async Task DeletingAnItemRemovesTheWholeSeries()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "Stretch",
            kind = "routine",
            category = "fitness",
            priority = "normal",
            startDate = LocalDate,
            recurrence = new { frequency = "daily", interval = 1 },
        });
        var created = await DailyApi.ReadAsync<PlanningItemDto>(response);
        await SetStatusAsync(client, created!.Id, LocalDate, "completed");

        var deleted = await DailyApi.SendAsync(
            client,
            HttpMethod.Delete,
            $"/api/calendar/items/{created.Id}");

        var day = await DailyApi.GetAsync<CalendarDayDto>(
            client,
            $"/api/calendar/day?date={LocalDate}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(day!.Occurrences);
    }

    [Fact]
    public async Task ValidationFailuresComeBackAsSanitizedProblemDetails()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title = "   ",
            kind = "task",
            category = "general",
            priority = "normal",
            startDate = LocalDate,
            startTime = "10:00",
            endTime = "09:00",
        });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("title"));
        Assert.True(problem.Errors.ContainsKey("endTime"));
        // The messages describe the rule, never the value that was submitted, so a validation
        // response cannot echo private text back through an error.
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("traceId", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WritesRequireAValidAntiforgeryToken()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var missing = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/calendar/items",
            NewItem());

        var invalid = await DailyApi.SendWithInvalidAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/calendar/items",
            NewItem());

        var valid = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/calendar/items",
            NewItem());

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
    }

    [Fact]
    public async Task RecordingAnOccurrenceStatusRequiresAValidAntiforgeryToken()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var id = await CreateItemAsync(client);
        var url = $"/api/calendar/items/{id}/occurrences/{LocalDate}/status";

        var missing = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Put,
            url,
            new { status = "completed" });

        var invalid = await DailyApi.SendWithInvalidAntiforgeryAsync(
            client,
            HttpMethod.Put,
            url,
            new { status = "completed" });

        var valid = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            url,
            new { status = "completed" });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task OneAccountCannotSeeOrTouchAnotherAccountsItem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        var id = await CreateItemAsync(clientA);

        var day = await DailyApi.GetAsync<CalendarDayDto>(
            clientB,
            $"/api/calendar/day?date={LocalDate}");
        var read = await clientB.GetAsync($"/api/calendar/items/{id}");
        var update = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Put,
            $"/api/calendar/items/{id}",
            NewItem());
        var status = await SetStatusAsync(clientB, id, LocalDate, "completed");
        var delete = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Delete,
            $"/api/calendar/items/{id}");

        Assert.Empty(day!.Occurrences);
        // Answering "not found" rather than "forbidden" refuses to confirm that the identifier
        // names something real.
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task CalendarResponsesAreNoStoreAndExposeNoAccountIdentifier()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await CreateItemAsync(client);

        var responses = new[]
        {
            await client.GetAsync("/api/calendar/month?year=2026&month=7"),
            await client.GetAsync($"/api/calendar/day?date={LocalDate}"),
            await client.GetAsync("/api/calendar/upcoming"),
        };

        foreach (var response in responses)
        {
            var body = await response.Content.ReadAsStringAsync();

            // A calendar says where somebody will be and when, so no proxy or shared browser may
            // keep a copy of one.
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<Guid> CreateItemAsync(
        HttpClient client,
        string title = "Dentist",
        string kind = "appointment",
        string priority = "normal")
    {
        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/calendar/items", new
        {
            title,
            kind,
            category = "health",
            priority,
            startDate = LocalDate,
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

    private static object NewItem() => new
    {
        title = "Dentist",
        kind = "appointment",
        category = "health",
        priority = "normal",
        startDate = LocalDate,
    };
}
