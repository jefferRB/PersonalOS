using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The study project and study session endpoints.
/// </summary>
public sealed class StudyEndpointsTests
{
    private const string Monday = "2026-07-27";

    [Fact]
    public async Task EveryStudyRouteRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync("/api/study/projects"),
            await client.GetAsync($"/api/study/sessions?from={Monday}&to={Monday}"),
            await DailyApi.SendAsync(client, HttpMethod.Post, "/api/study/projects", Project()),
            await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/study/projects/{id}", Project()),
            await DailyApi.SendAsync(client, HttpMethod.Post, "/api/study/sessions", new
            {
                studyProjectId = id,
                localDate = Monday,
                durationMinutes = 45,
            }),
            await DailyApi.SendAsync(client, HttpMethod.Delete, $"/api/study/sessions/{id}"),
        };

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task AProjectStoresItsResourceMetadata()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var project = await CreateProjectAsync(client, "https://angular.dev/guide/signals");

        Assert.Equal("Angular", project.Name);
        Assert.Single(project.Resources);
        Assert.Equal("https://angular.dev/guide/signals", project.Resources[0].ExternalUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("angular.dev")]
    public async Task AResourceLinkThatIsNotHttpOrHttpsIsRejected(string url)
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/study/projects",
            Project(url));
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);
        var projects = await DailyApi.GetAsync<List<StudyProjectDto>>(client, "/api/study/projects");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("resources"));
        // Nothing was stored, so no template can ever render the rejected scheme.
        Assert.Empty(projects!);
    }

    [Fact]
    public async Task StudySessionsAggregateAcrossTheWeekAndCarryTheProjectName()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var project = await CreateProjectAsync(client);

        await CreateSessionAsync(client, project.Id, Monday, 45);
        await CreateSessionAsync(client, project.Id, "2026-07-30", 90);
        await CreateSessionAsync(client, project.Id, "2026-08-06", 60);

        var week = await DailyApi.GetAsync<List<StudySessionDto>>(
            client,
            $"/api/study/sessions?from={Monday}&to=2026-08-02");

        Assert.Equal(2, week!.Count);
        Assert.Equal(135, week.Sum(session => session.DurationMinutes));
        Assert.All(week, session => Assert.Equal("Angular", session.ProjectName));
    }

    [Fact]
    public async Task AStudySessionCanBeEditedAndDeleted()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var project = await CreateProjectAsync(client);
        var session = await CreateSessionAsync(client, project.Id, Monday, 45);

        var update = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/study/sessions/{session.Id}",
            new
            {
                studyProjectId = project.Id,
                localDate = Monday,
                durationMinutes = 75,
                progressNote = "Finished the signals chapter.",
            });
        var updated = await DailyApi.ReadAsync<StudySessionDto>(update);

        var delete = await DailyApi.SendAsync(
            client,
            HttpMethod.Delete,
            $"/api/study/sessions/{session.Id}");
        var remaining = await DailyApi.GetAsync<List<StudySessionDto>>(
            client,
            $"/api/study/sessions?from={Monday}&to={Monday}");

        Assert.Equal(75, updated!.DurationMinutes);
        Assert.Equal("Finished the signals chapter.", updated.ProgressNote);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(remaining!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public async Task AnUnusableDurationIsRejected(int minutes)
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var project = await CreateProjectAsync(client);

        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/study/sessions", new
        {
            studyProjectId = project.Id,
            localDate = Monday,
            durationMinutes = minutes,
        });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("durationMinutes"));
    }

    [Fact]
    public async Task ASessionCannotBeAttachedToAnotherAccountsProject()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var projectA = await CreateProjectAsync(clientA);

        var response = await DailyApi.SendAsync(clientB, HttpMethod.Post, "/api/study/sessions", new
        {
            studyProjectId = projectA.Id,
            localDate = Monday,
            durationMinutes = 45,
        });
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("studyProjectId"));
    }

    [Fact]
    public async Task AStudyWriteWithoutAnAntiforgeryTokenIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/study/projects",
            Project());
        var projects = await DailyApi.GetAsync<List<StudyProjectDto>>(client, "/api/study/projects");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(projects!);
    }

    [Fact]
    public async Task TwoAccountsNeverSeeEachOthersProjectsOrSessions()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var projectA = await CreateProjectAsync(clientA);
        await CreateSessionAsync(clientA, projectA.Id, Monday, 45);

        var projectsB = await DailyApi.GetAsync<List<StudyProjectDto>>(
            clientB,
            "/api/study/projects");
        var sessionsB = await DailyApi.GetAsync<List<StudySessionDto>>(
            clientB,
            $"/api/study/sessions?from={Monday}&to={Monday}");
        var updateAttempt = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Put,
            $"/api/study/projects/{projectA.Id}",
            Project());

        Assert.Empty(projectsB!);
        Assert.Empty(sessionsB!);
        Assert.Equal(HttpStatusCode.NotFound, updateAttempt.StatusCode);
    }

    private static async Task<StudyProjectDto> CreateProjectAsync(
        HttpClient client,
        string? url = null)
    {
        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/study/projects",
            Project(url));
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<StudyProjectDto>(response))!;
    }

    private static async Task<StudySessionDto> CreateSessionAsync(
        HttpClient client,
        Guid projectId,
        string date,
        int minutes)
    {
        var response = await DailyApi.SendAsync(client, HttpMethod.Post, "/api/study/sessions", new
        {
            studyProjectId = projectId,
            localDate = date,
            durationMinutes = minutes,
        });
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<StudySessionDto>(response))!;
    }

    private static object Project(string? url = null) => new
    {
        name = "Angular",
        description = (string?)null,
        status = "active",
        resources = url is null
            ? Array.Empty<object>()
            : [new { title = "Signals guide", resourceType = "article", externalUrl = url }],
    };
}

/// <summary>
/// The journal endpoints, including the controls that protect the most sensitive data.
/// </summary>
public sealed class JournalEndpointsTests
{
    private const string LocalDate = "2026-07-30";

    [Fact]
    public async Task TheJournalRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);

        var read = await client.GetAsync($"/api/journal/{LocalDate}");
        var write = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Private."));

        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, write.StatusCode);
    }

    [Fact]
    public async Task ADayWithNoEntryReadsAsEmptyRatherThanAsAnError()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var entry = await DailyApi.GetAsync<JournalEntryDto>(client, $"/api/journal/{LocalDate}");

        Assert.False(entry!.HasContent);
        Assert.Null(entry.WentWell);
    }

    [Fact]
    public async Task AReflectionIsSavedAndSurvivesAReload()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Shipped the milestone."));

        var entry = await DailyApi.GetAsync<JournalEntryDto>(client, $"/api/journal/{LocalDate}");

        Assert.Equal("Shipped the milestone.", entry!.WentWell);
        Assert.True(entry.HasContent);
    }

    [Fact]
    public async Task SavingTheSameDayTwiceUpdatesTheEntryRatherThanDuplicatingIt()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("First version."));
        var second = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Second version."));

        var entry = await DailyApi.GetAsync<JournalEntryDto>(client, $"/api/journal/{LocalDate}");

        // A unique index on account and local date means a duplicate could not exist.
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("Second version.", entry!.WentWell);
    }

    [Fact]
    public async Task JournalResponsesUseNoStoreCaching()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var read = await client.GetAsync($"/api/journal/{LocalDate}");
        var write = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Private."));

        // A shared browser or a proxy must never keep a copy of a reflection.
        Assert.True(read.Headers.CacheControl?.NoStore);
        Assert.True(write.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task AJournalWriteWithoutAnAntiforgeryTokenIsRejectedAndStoresNothing()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Should not be stored."));
        var entry = await DailyApi.GetAsync<JournalEntryDto>(client, $"/api/journal/{LocalDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(entry!.HasContent);
    }

    [Fact]
    public async Task ASectionLongerThanTheColumnIsRejectedWithoutEchoingTheText()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var secret = new string('x', 4001);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry(secret));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnOverPostedDateOrAccountInTheBodyIsIgnored()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(clientA, HttpMethod.Put, $"/api/journal/{LocalDate}", new
        {
            wentWell = "Mine.",
            // Neither of these may redirect the write.
            localDate = "2020-01-01",
            userId = Guid.NewGuid(),
        });

        var onTheRequestedDay = await DailyApi.GetAsync<JournalEntryDto>(
            clientA,
            $"/api/journal/{LocalDate}");
        var onTheSmuggledDay = await DailyApi.GetAsync<JournalEntryDto>(
            clientA,
            "/api/journal/2020-01-01");
        var forAccountB = await DailyApi.GetAsync<JournalEntryDto>(
            clientB,
            $"/api/journal/{LocalDate}");

        Assert.Equal("Mine.", onTheRequestedDay!.WentWell);
        Assert.False(onTheSmuggledDay!.HasContent);
        Assert.False(forAccountB!.HasContent);
    }

    [Fact]
    public async Task OneAccountCannotReadAnotherAccountsReflection()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(
            clientA,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Something only account A wrote."));

        var response = await clientB.GetAsync($"/api/journal/{LocalDate}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("only account A", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TodayReportsOnlyWhetherTheReflectionExists()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/journal/{LocalDate}",
            Entry("Something private."));

        var response = await client.GetAsync($"/api/today?date={LocalDate}");
        var summary = await DailyApi.ReadAsync<TodaySummaryDto>(response);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(summary!.Progress.JournalCompleted);
        // Whether a day was reflected on is not private in the way the reflection is.
        Assert.DoesNotContain("Something private", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkupInAReflectionIsStoredAndReturnedAsPlainText()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        const string Payload = "<img src=x onerror=\"alert(1)\">";

        await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/journal/{LocalDate}", Entry(Payload));

        var response = await client.GetAsync($"/api/journal/{LocalDate}");
        var entry = await DailyApi.ReadAsync<JournalEntryDto>(response);

        // The server stores exactly what was written and returns JSON, never HTML. Angular renders
        // it through interpolation, so the markup is shown as characters.
        Assert.Equal(Payload, entry!.WentWell);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    private static object Entry(string wentWell) => new { wentWell };
}
