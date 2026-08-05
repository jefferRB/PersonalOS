using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PersonalOS.IntegrationTests;

public sealed class TimeEndpointsTests
{
    private const string StrongPassword = "Password123";

    [Fact]
    public async Task TimeContext_WithoutSession_ReturnsUnauthorized()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/time/context");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimeContext_ForANewAccount_UsesUtc()
    {
        await using var factory = new PersonalOSWebApplicationFactory
        {
            UtcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero),
        };
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var context = await client.GetFromJsonAsync<TimeContextResponse>("/api/time/context");

        Assert.NotNull(context);
        Assert.Equal("UTC", context.TimeZoneId);
        Assert.Equal(0, context.UtcOffsetMinutes);
        Assert.Equal(new DateOnly(2026, 7, 30), context.LocalDate);
        Assert.Equal(factory.UtcNow, context.UtcNow);
    }

    [Fact]
    public async Task TimeContext_UsesThePersistedTimeZone()
    {
        await using var factory = new PersonalOSWebApplicationFactory
        {
            UtcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero),
        };
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Jefferson", "America/Costa_Rica");

        var context = await client.GetFromJsonAsync<TimeContextResponse>("/api/time/context");

        Assert.NotNull(context);
        Assert.Equal("America/Costa_Rica", context.TimeZoneId);
        Assert.Equal(-360, context.UtcOffsetMinutes);
        Assert.Equal(13, context.LocalNow.Hour);
        Assert.Equal(new DateOnly(2026, 7, 30), context.LocalDate);
    }

    [Fact]
    public async Task TimeContext_NearTheUtcDayBoundary_ReturnsThePersistedLocalDate()
    {
        // 00:30 UTC on 31 July is still 30 July in Costa Rica.
        await using var factory = new PersonalOSWebApplicationFactory
        {
            UtcNow = new DateTimeOffset(2026, 7, 31, 0, 30, 0, TimeSpan.Zero),
        };
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Jefferson", "America/Costa_Rica");

        var context = await client.GetFromJsonAsync<TimeContextResponse>("/api/time/context");

        Assert.NotNull(context);
        Assert.Equal(new DateOnly(2026, 7, 30), context.LocalDate);
        Assert.Equal(new DateOnly(2026, 7, 31), DateOnly.FromDateTime(context.UtcNow.UtcDateTime));
    }

    [Fact]
    public async Task TimeContext_ReturnsIsoCompatibleValuesAndNoLocalizedText()
    {
        await using var factory = new PersonalOSWebApplicationFactory
        {
            UtcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero),
        };
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await UpdateProfileAsync(client, "Jefferson", "America/Costa_Rica");

        var response = await client.GetAsync("/api/time/context");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"localDate\":\"2026-07-30\"", body, StringComparison.Ordinal);
        Assert.Contains("2026-07-30T13:24:00-06:00", body, StringComparison.Ordinal);
        // The server never formats a weekday or month name; the client does that.
        Assert.DoesNotContain("Thursday", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("July", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jueves", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimeContext_UsesNoStoreCaching()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await client.GetAsync("/api/time/context");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task TimeContext_ForTwoAccounts_ReflectsEachPersistedTimeZone()
    {
        // 23:30 UTC is still 30 July in Costa Rica but already 31 July in Tokyo.
        await using var factory = new PersonalOSWebApplicationFactory
        {
            UtcNow = new DateTimeOffset(2026, 7, 30, 23, 30, 0, TimeSpan.Zero),
        };
        using var clientA = CreateClient(factory);
        using var clientB = CreateClient(factory);
        var emailA = NewEmail();
        var emailB = NewEmail();

        await RegisterAsync(clientA, emailA);
        await RegisterAsync(clientB, emailB);
        await LoginAsync(clientA, emailA);
        await LoginAsync(clientB, emailB);

        await UpdateProfileAsync(clientA, "Account A", "America/Costa_Rica");
        await UpdateProfileAsync(clientB, "Account B", "Asia/Tokyo");

        var contextA = await clientA.GetFromJsonAsync<TimeContextResponse>("/api/time/context");
        var contextB = await clientB.GetFromJsonAsync<TimeContextResponse>("/api/time/context");

        Assert.Equal(new DateOnly(2026, 7, 30), contextA!.LocalDate);
        Assert.Equal(new DateOnly(2026, 7, 31), contextB!.LocalDate);
        Assert.Equal(contextA.UtcNow, contextB.UtcNow);
    }

    private static HttpClient CreateClient(PersonalOSWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task UpdateProfileAsync(
        HttpClient client,
        string displayName,
        string timeZoneId)
    {
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PutAsJsonAsync("/api/profile", new { displayName, timeZoneId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        await AddAntiforgeryHeaderAsync(client);

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            displayName = "Jefferson",
            email,
            password = StrongPassword,
        });
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        await AddAntiforgeryHeaderAsync(client);

        await client.PostAsJsonAsync("/api/auth/login", new
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

    private sealed record TimeContextResponse(
        DateTimeOffset UtcNow,
        DateTimeOffset LocalNow,
        DateOnly LocalDate,
        string TimeZoneId,
        int UtcOffsetMinutes);
}
