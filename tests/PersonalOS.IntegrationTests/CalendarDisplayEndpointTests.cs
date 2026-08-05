using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The calendar display preference endpoint, exercised through the real HTTP pipeline.
/// </summary>
public sealed class CalendarDisplayEndpointTests
{
    private const string Route = "/api/profile/calendar-display";

    [Fact]
    public async Task AnAccountThatHasNeverChosenSeesTheDefaults()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var profile = await DailyApi.GetAsync<UserProfileDto>(client, "/api/profile");

        Assert.Equal(new TimeOnly(6, 0), profile!.CalendarDisplay.DayStartTime);
        Assert.Equal(new TimeOnly(22, 0), profile.CalendarDisplay.DayEndTime);
        Assert.Equal(15, profile.CalendarDisplay.SlotMinutes);
    }

    [Fact]
    public async Task AValidWindowIsSavedAndReadBack()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Put, Route, new
        {
            dayStartTime = "07:30",
            dayEndTime = "19:00",
            slotMinutes = 30,
        });

        var saved = await DailyApi.ReadAsync<UserProfileDto>(response);
        var reread = await DailyApi.GetAsync<UserProfileDto>(client, "/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new TimeOnly(7, 30), saved!.CalendarDisplay.DayStartTime);
        Assert.Equal(30, reread!.CalendarDisplay.SlotMinutes);
        Assert.Equal(new TimeOnly(19, 0), reread.CalendarDisplay.DayEndTime);
    }

    [Fact]
    public async Task AStartThatIsNotEarlierThanTheEndIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Put, Route, new
        {
            dayStartTime = "22:00",
            dayEndTime = "06:00",
            slotMinutes = 15,
        });

        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);
        var profile = await DailyApi.GetAsync<UserProfileDto>(client, "/api/profile");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("dayEndTime"));
        // The refused values are never written, so the planner keeps drawing what it drew before.
        Assert.Equal(new TimeOnly(6, 0), profile!.CalendarDisplay.DayStartTime);
    }

    [Fact]
    public async Task AnIntervalTheGridCannotDrawIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Put, Route, new
        {
            dayStartTime = "06:00",
            dayEndTime = "22:00",
            slotMinutes = 7,
        });

        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("slotMinutes"));
    }

    [Fact]
    public async Task SavingTheWindowLeavesTheDisplayNameAndTimeZoneAlone()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/profile", new
        {
            displayName = "Jefferson",
            timeZoneId = "America/Costa_Rica",
        });

        await DailyApi.SendAsync(client, HttpMethod.Put, Route, new
        {
            dayStartTime = "08:00",
            dayEndTime = "18:00",
            slotMinutes = 60,
        });

        var profile = await DailyApi.GetAsync<UserProfileDto>(client, "/api/profile");

        Assert.Equal("Jefferson", profile!.DisplayName);
        Assert.Equal("America/Costa_Rica", profile.TimeZoneId);
        Assert.Equal(60, profile.CalendarDisplay.SlotMinutes);
    }

    [Fact]
    public async Task OneAccountsWindowIsInvisibleToAnother()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(clientA, HttpMethod.Put, Route, new
        {
            dayStartTime = "05:00",
            dayEndTime = "12:00",
            slotMinutes = 60,
        });

        var profileB = await DailyApi.GetAsync<UserProfileDto>(clientB, "/api/profile");

        // Ownership comes from the authentication cookie, so one account's toolbar can never reach
        // another account's preferences.
        Assert.Equal(new TimeOnly(6, 0), profileB!.CalendarDisplay.DayStartTime);
        Assert.Equal(15, profileB.CalendarDisplay.SlotMinutes);
    }

    [Fact]
    public async Task TheEndpointRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);

        var response = await DailyApi.SendAsync(client, HttpMethod.Put, Route, new
        {
            dayStartTime = "06:00",
            dayEndTime = "22:00",
            slotMinutes = 15,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TheEndpointRequiresAValidAntiforgeryToken()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var body = new { dayStartTime = "07:00", dayEndTime = "20:00", slotMinutes = 30 };

        var missing = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Put,
            Route,
            body);

        var invalid = await DailyApi.SendWithInvalidAntiforgeryAsync(
            client,
            HttpMethod.Put,
            Route,
            body);

        var valid = await DailyApi.SendAsync(client, HttpMethod.Put, Route, body);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }
}
