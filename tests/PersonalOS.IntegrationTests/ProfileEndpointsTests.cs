using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PersonalOS.IntegrationTests;

public sealed class ProfileEndpointsTests
{
    private const string StrongPassword = "Password123";

    [Fact]
    public async Task GetProfile_WithoutSession_ReturnsUnauthorized()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProfile_WithoutSession_ReturnsUnauthorized()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            displayName = "Anonymous",
            timeZoneId = "UTC",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithSession_ReturnsDefaultPreferencesForANewAccount()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await client.GetAsync("/api/profile");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal("Jefferson", profile.DisplayName);
        Assert.Equal(email, profile.Email);
        Assert.Equal("UTC", profile.TimeZoneId);
    }

    [Fact]
    public async Task UpdateProfile_WithValidValues_ReturnsTheUpdatedProfile()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await UpdateProfileAsync(client, "Jefferson Rojas", "America/Costa_Rica");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal("Jefferson Rojas", profile.DisplayName);
        Assert.Equal("America/Costa_Rica", profile.TimeZoneId);
        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public async Task UpdateProfile_PersistsAcrossRequests()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Jefferson Rojas", "America/Costa_Rica");

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");

        Assert.NotNull(profile);
        Assert.Equal("Jefferson Rojas", profile.DisplayName);
        Assert.Equal("America/Costa_Rica", profile.TimeZoneId);
    }

    [Fact]
    public async Task UpdateProfile_SurvivesANewSession()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Persisted Name", "Europe/Madrid");

        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsync("/api/auth/logout", content: null);
        await LoginAsync(client, email);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");

        Assert.NotNull(profile);
        Assert.Equal("Persisted Name", profile.DisplayName);
        Assert.Equal("Europe/Madrid", profile.TimeZoneId);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAntiforgeryToken_IsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            displayName = "Should Not Save",
            timeZoneId = "America/Costa_Rica",
        });
        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Jefferson", profile!.DisplayName);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidAntiforgeryToken_IsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid-request-token");

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            displayName = "Should Not Save",
            timeZoneId = "America/Costa_Rica",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("Central America Standard Time")]
    [InlineData("-06:00")]
    [InlineData("")]
    public async Task UpdateProfile_WithInvalidTimeZone_ReturnsFieldValidationProblem(
        string timeZoneId)
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await UpdateProfileAsync(client, "Jefferson", timeZoneId);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problem?.Errors);
        Assert.True(problem.Errors.ContainsKey("timeZoneId"));
        // The response must not leak host time-zone internals.
        Assert.DoesNotContain("TimeZoneNotFound", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Registry", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at PersonalOS", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData("J")]
    public async Task UpdateProfile_WithUnacceptableDisplayName_ReturnsFieldValidationProblem(
        string displayName)
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await UpdateProfileAsync(client, displayName, "UTC");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem?.Errors);
        Assert.True(problem.Errors.ContainsKey("displayName"));
    }

    [Fact]
    public async Task UpdateProfile_TrimsTheDisplayNameBeforeSaving()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await UpdateProfileAsync(client, "   Trimmed Name   ", "UTC");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal("Trimmed Name", profile!.DisplayName);
    }

    [Fact]
    public async Task UpdateProfile_IgnoresOverPostedEmailAndAccountIdentifier()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            displayName = "Jefferson Rojas",
            timeZoneId = "America/Costa_Rica",
            email = "attacker@example.com",
            userId = Guid.NewGuid(),
            id = Guid.NewGuid(),
            passwordHash = "injected",
            emailConfirmed = true,
        });
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, profile!.Email);
        Assert.Equal("Jefferson Rojas", profile.DisplayName);

        // The original sign-in address must still work.
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsync("/api/auth/logout", content: null);
        var loginResponse = await LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_IsReflectedByTheCurrentUserEndpoint()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Renamed User", "Asia/Tokyo");

        var currentUser = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.NotNull(currentUser);
        Assert.Equal("Renamed User", currentUser.DisplayName);
        Assert.Equal(email, currentUser.Email);
    }

    [Fact]
    public async Task ProfileResponse_ExcludesSensitiveIdentityFields()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await client.GetAsync("/api/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConcurrencyStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccessFailedCount", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LockoutEnd", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NormalizedEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileResponses_UseNoStoreCaching()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var readResponse = await client.GetAsync("/api/profile");
        var updateResponse = await UpdateProfileAsync(client, "Jefferson", "UTC");

        Assert.True(readResponse.Headers.CacheControl?.NoStore);
        Assert.True(updateResponse.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task TwoAccounts_KeepIndependentProfiles()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = CreateClient(factory);
        using var clientB = CreateClient(factory);
        var emailA = NewEmail();
        var emailB = NewEmail();

        await RegisterAsync(clientA, emailA);
        await RegisterAsync(clientB, emailB);
        await LoginAsync(clientA, emailA);
        await LoginAsync(clientB, emailB);

        await UpdateProfileAsync(clientA, "Account A", "America/Costa_Rica");

        var profileA = await clientA.GetFromJsonAsync<ProfileResponse>("/api/profile");
        var profileB = await clientB.GetFromJsonAsync<ProfileResponse>("/api/profile");

        Assert.Equal("Account A", profileA!.DisplayName);
        Assert.Equal("America/Costa_Rica", profileA.TimeZoneId);
        Assert.Equal(emailA, profileA.Email);

        Assert.Equal("Jefferson", profileB!.DisplayName);
        Assert.Equal("UTC", profileB.TimeZoneId);
        Assert.Equal(emailB, profileB.Email);
    }

    [Fact]
    public async Task OneAccountCannotSelectAnotherAccountThroughRequestData()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = CreateClient(factory);
        using var clientB = CreateClient(factory);
        var emailA = NewEmail();
        var emailB = NewEmail();

        await RegisterAsync(clientA, emailA);
        await RegisterAsync(clientB, emailB);
        await LoginAsync(clientA, emailA);
        await LoginAsync(clientB, emailB);

        var accountB = await clientB.GetFromJsonAsync<ProfileResponse>("/api/profile");
        var currentUserB = await clientB.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.NotNull(accountB);
        Assert.NotNull(currentUserB);

        // Attempt to address account B through the body, the query string, and the route.
        await AddAntiforgeryHeaderAsync(clientA);
        var bodyAttempt = await clientA.PutAsJsonAsync("/api/profile", new
        {
            displayName = "Hijacked",
            timeZoneId = "Asia/Tokyo",
            userId = currentUserB.Id,
            id = currentUserB.Id,
            email = emailB,
        });

        await AddAntiforgeryHeaderAsync(clientA);
        var queryAttempt = await clientA.PutAsJsonAsync(
            $"/api/profile?userId={currentUserB.Id}",
            new { displayName = "Hijacked By Query", timeZoneId = "Asia/Tokyo" });

        await AddAntiforgeryHeaderAsync(clientA);
        var routeAttempt = await clientA.PutAsJsonAsync(
            $"/api/profile/{currentUserB.Id}",
            new { displayName = "Hijacked By Route", timeZoneId = "Asia/Tokyo" });

        var readAttempt = await clientA.GetAsync($"/api/profile/{currentUserB.Id}");

        // Account A only ever changed its own profile.
        Assert.Equal(HttpStatusCode.OK, bodyAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, queryAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, routeAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readAttempt.StatusCode);

        var profileA = await clientA.GetFromJsonAsync<ProfileResponse>("/api/profile");
        var profileB = await clientB.GetFromJsonAsync<ProfileResponse>("/api/profile");

        Assert.Equal(emailA, profileA!.Email);
        Assert.Equal("Hijacked By Query", profileA.DisplayName);

        Assert.Equal(emailB, profileB!.Email);
        Assert.Equal(accountB.DisplayName, profileB.DisplayName);
        Assert.Equal("UTC", profileB.TimeZoneId);
    }

    [Fact]
    public async Task ExistingAccountWithoutPreferences_ReadsTheDefaultTimeZoneWithoutFailing()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var currentUser = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.NotNull(currentUser);

        // Simulate a Milestone 1 account that predates the preferences table.
        await factory.RemovePreferencesAsync(currentUser.Id);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");
        var timeContext = await client.GetFromJsonAsync<TimeContextResponse>("/api/time/context");

        Assert.Equal("UTC", profile!.TimeZoneId);
        Assert.Equal("UTC", timeContext!.TimeZoneId);

        // Reading must not have created the record; saving must create it.
        Assert.False(await factory.HasPreferencesAsync(currentUser.Id));

        await UpdateProfileAsync(client, "Jefferson", "America/Costa_Rica");

        Assert.True(await factory.HasPreferencesAsync(currentUser.Id));
    }

    private static HttpClient CreateClient(PersonalOSWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task<HttpResponseMessage> UpdateProfileAsync(
        HttpClient client,
        string displayName,
        string timeZoneId)
    {
        await AddAntiforgeryHeaderAsync(client);

        return await client.PutAsJsonAsync("/api/profile", new { displayName, timeZoneId });
    }

    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        await AddAntiforgeryHeaderAsync(client);

        return await client.PostAsJsonAsync("/api/auth/register", new
        {
            displayName = "Jefferson",
            email,
            password = StrongPassword,
        });
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        await AddAntiforgeryHeaderAsync(client);

        return await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = StrongPassword,
            rememberMe = false,
        });
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/antiforgery/token");

        Assert.NotNull(response);

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", response.RequestToken);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private sealed record AntiforgeryTokenResponse(string RequestToken);

    private sealed record CurrentUserResponse(Guid Id, string DisplayName, string Email);

    private sealed record ProfileResponse(
        string DisplayName,
        string Email,
        string TimeZoneId,
        DateTimeOffset UpdatedAtUtc);

    private sealed record TimeContextResponse(
        DateTimeOffset UtcNow,
        DateTimeOffset LocalNow,
        DateOnly LocalDate,
        string TimeZoneId,
        int UtcOffsetMinutes);

    private sealed record ValidationProblem(
        string? Title,
        int? Status,
        Dictionary<string, string[]>? Errors);
}
