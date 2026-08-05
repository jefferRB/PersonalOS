using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The routine and routine-session endpoints.
/// </summary>
public sealed class RoutineEndpointsTests
{
    /// <summary>Monday 27 July 2026.</summary>
    private const string Monday = "2026-07-27";

    [Fact]
    public async Task EveryRoutineRouteRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync("/api/routines"),
            await client.GetAsync($"/api/routines/{id}"),
            await client.GetAsync($"/api/routines/occurrences?from={Monday}&to={Monday}"),
            await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", ChestWorkout()),
            await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/routines/{id}", ChestWorkout()),
            await DailyApi.SendAsync(client, HttpMethod.Delete, $"/api/routines/{id}"),
            await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/routine-sessions/{id}", new
            {
                isCompleted = true,
            }),
        };

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task ARoutineIsCreatedWithItsOrderedSteps()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var routine = await CreateRoutineAsync(client);

        Assert.Equal("Monday - Chest", routine.Name);
        Assert.Equal(
            ["Bench press", "Incline dumbbell press", "Pec deck"],
            routine.Steps.Select(step => step.Title));
        Assert.Equal([0, 1, 2], routine.Steps.Select(step => step.Order));
        Assert.Equal(60m, routine.Steps[0].TargetWeight);
    }

    [Fact]
    public async Task OccurrencesAppearOnEveryMatchingDayWithoutCreatingRows()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await CreateRoutineAsync(client);

        var occurrences = await DailyApi.GetAsync<List<RoutineOccurrenceDto>>(
            client,
            $"/api/routines/occurrences?from={Monday}&to=2026-08-23");

        // Four Mondays: 27 July, 3, 10, and 17 August.
        Assert.Equal(4, occurrences!.Count);
        Assert.Equal(
            [
                new DateOnly(2026, 7, 27),
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 17),
            ],
            occurrences.Select(occurrence => occurrence.LocalDate));
        Assert.All(occurrences, occurrence => Assert.Null(occurrence.SessionId));
    }

    [Fact]
    public async Task ARoutineDoesNotAppearOnADayItsRuleDoesNotMatch()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await CreateRoutineAsync(client);

        var tuesday = await DailyApi.GetAsync<List<RoutineOccurrenceDto>>(
            client,
            "/api/routines/occurrences?from=2026-07-28&to=2026-07-28");

        Assert.Empty(tuesday!);
    }

    [Fact]
    public async Task SelectedWeekdaysProduceOccurrencesOnEveryChosenDay()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", new
        {
            name = "Gym",
            category = "workout",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new
            {
                frequency = "selectedWeekdays",
                interval = 1,
                startDate = Monday,
                selectedWeekdays = new[] { "monday", "wednesday", "friday" },
            },
        });

        var occurrences = await DailyApi.GetAsync<List<RoutineOccurrenceDto>>(
            client,
            $"/api/routines/occurrences?from={Monday}&to=2026-08-02");

        Assert.Equal(
            [
                new DateOnly(2026, 7, 27),
                new DateOnly(2026, 7, 29),
                new DateOnly(2026, 7, 31),
            ],
            occurrences!.Select(occurrence => occurrence.LocalDate));
    }

    [Fact]
    public async Task AWorkoutSessionRecordsSetsRepetitionsAndWeightThatSurviveAReload()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var routine = await CreateRoutineAsync(client);

        var session = await StartSessionAsync(client, routine.Id);
        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/routine-sessions/{session.Id}",
            new
            {
                isCompleted = false,
                stepResults = new[]
                {
                    new
                    {
                        routineStepId = routine.Steps[0].Id,
                        isCompleted = true,
                        actualSets = 4,
                        actualRepetitions = 8,
                        actualWeight = 62.5m,
                    },
                },
            });

        var reloaded = await DailyApi.GetAsync<RoutineSessionDto>(
            client,
            $"/api/routine-sessions/{session.Id}");
        var result = reloaded!.StepResults.Single(
            item => item.RoutineStepId == routine.Steps[0].Id);

        Assert.True(result.IsCompleted);
        Assert.Equal(4, result.ActualSets);
        Assert.Equal(8, result.ActualRepetitions);
        Assert.Equal(62.5m, result.ActualWeight);
        Assert.Null(reloaded.CompletedAtUtc);
    }

    [Fact]
    public async Task CompletingASessionIsReflectedByTheOccurrenceForThatDay()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var routine = await CreateRoutineAsync(client);
        var session = await StartSessionAsync(client, routine.Id);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/routine-sessions/{session.Id}",
            new { isCompleted = true, stepResults = Array.Empty<object>() });

        var occurrences = await DailyApi.GetAsync<List<RoutineOccurrenceDto>>(
            client,
            $"/api/routines/occurrences?from={Monday}&to={Monday}");

        Assert.True(occurrences![0].IsCompleted);
        Assert.Equal(session.Id, occurrences[0].SessionId);
    }

    [Fact]
    public async Task StartingTheSameDayTwiceReturnsTheSameSession()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var routine = await CreateRoutineAsync(client);

        var first = await StartSessionAsync(client, routine.Id);
        var second = await StartSessionAsync(client, routine.Id);

        // The unique index on routine and local date means a second row could not exist anyway.
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task AStepFromAnotherRoutineIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var routine = await CreateRoutineAsync(client);
        var session = await StartSessionAsync(client, routine.Id);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/routine-sessions/{session.Id}",
            new
            {
                isCompleted = false,
                stepResults = new[]
                {
                    new { routineStepId = Guid.NewGuid(), isCompleted = true },
                },
            });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("stepResults"));
    }

    [Fact]
    public async Task ARoutineWriteWithoutAnAntiforgeryTokenIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/routines",
            ChestWorkout());
        var routines = await DailyApi.GetAsync<List<RoutineDto>>(client, "/api/routines");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(routines!);
    }

    [Fact]
    public async Task ARoutineWithoutANameReturnsAFieldValidationProblem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", new
        {
            name = "   ",
            category = "workout",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new { frequency = "weekly", interval = 1, startDate = Monday },
        });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("name"));
    }

    [Fact]
    public async Task SelectedWeekdaysWithNoWeekdayReturnsAFieldValidationProblem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/routines", new
        {
            name = "Gym",
            category = "workout",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new
            {
                frequency = "selectedWeekdays",
                interval = 1,
                startDate = Monday,
                selectedWeekdays = Array.Empty<string>(),
            },
        });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("recurrence.selectedWeekdays"));
    }

    [Fact]
    public async Task OverPostedOwnershipFieldsAreIgnoredOnARoutine()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(clientA, HttpMethod.Post, "/api/routines", new
        {
            name = "Mine",
            category = "workout",
            isActive = true,
            steps = Array.Empty<object>(),
            recurrence = new { frequency = "weekly", interval = 1, startDate = Monday },
            userId = Guid.NewGuid(),
            id = Guid.NewGuid(),
        });

        var routinesB = await DailyApi.GetAsync<List<RoutineDto>>(clientB, "/api/routines");

        Assert.Empty(routinesB!);
    }

    [Fact]
    public async Task AnotherAccountsRoutineReturnsNotFound()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var routine = await CreateRoutineAsync(clientA);

        var read = await clientB.GetAsync($"/api/routines/{routine.Id}");
        var update = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Put,
            $"/api/routines/{routine.Id}",
            ChestWorkout());
        var start = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Post,
            $"/api/routines/{routine.Id}/sessions",
            new { localDate = Monday });
        var delete = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Delete,
            $"/api/routines/{routine.Id}");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task AnotherAccountsSessionReturnsNotFound()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var routine = await CreateRoutineAsync(clientA);
        var session = await StartSessionAsync(clientA, routine.Id);

        var read = await clientB.GetAsync($"/api/routine-sessions/{session.Id}");
        var save = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Put,
            $"/api/routine-sessions/{session.Id}",
            new { isCompleted = true, stepResults = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, save.StatusCode);
    }

    [Fact]
    public async Task DeactivatingARoutineRemovesItFromOccurrencesButKeepsIt()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var routine = await CreateRoutineAsync(client);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/routines/{routine.Id}",
            ChestWorkout() with { isActive = false });

        var occurrences = await DailyApi.GetAsync<List<RoutineOccurrenceDto>>(
            client,
            $"/api/routines/occurrences?from={Monday}&to={Monday}");
        var stored = await DailyApi.GetAsync<RoutineDto>(client, $"/api/routines/{routine.Id}");

        Assert.Empty(occurrences!);
        Assert.False(stored!.IsActive);
    }

    private static async Task<RoutineDto> CreateRoutineAsync(HttpClient client)
    {
        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/routines",
            ChestWorkout());
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<RoutineDto>(response))!;
    }

    private static async Task<RoutineSessionDto> StartSessionAsync(HttpClient client, Guid routineId)
    {
        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            $"/api/routines/{routineId}/sessions",
            new { localDate = Monday });
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<RoutineSessionDto>(response))!;
    }

    private static RoutineRequest ChestWorkout() =>
        new(
            "Monday - Chest",
            null,
            "workout",
            true,
            new RecurrenceRequestDto("weekly", 1, Monday, null, []),
            [
                new StepRequest("Bench press", "exercise", 3, 10, 60m, null, null),
                new StepRequest("Incline dumbbell press", "exercise", 3, 12, 22.5m, null, null),
                new StepRequest("Pec deck", "exercise", 3, 15, 40m, null, null),
            ]);

    private sealed record RoutineRequest(
        string name,
        string? description,
        string category,
        bool isActive,
        RecurrenceRequestDto recurrence,
        IReadOnlyList<StepRequest> steps);

    private sealed record RecurrenceRequestDto(
        string frequency,
        int interval,
        string startDate,
        string? endDate,
        IReadOnlyList<string> selectedWeekdays);

    private sealed record StepRequest(
        string title,
        string stepType,
        int? targetSets,
        int? targetRepetitions,
        decimal? targetWeight,
        int? targetDurationMinutes,
        string? notes);
}
