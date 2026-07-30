using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PersonalOS.IntegrationTests;

public sealed class AuthEndpointsTests
{
    private const string StrongPassword = "Password123";

    [Fact]
    public async Task Register_WithValidRequest_CreatesAccountWithoutSigningIn()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsValidationProblem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithInvalidRequest_ReturnsValidationProblem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            displayName = "",
            email = "not-an-email",
            password = "short",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsCurrentUser()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        var response = await LoginAsync(client, email, rememberMe: true);
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal("Jefferson", currentUser.DisplayName);
        Assert.Equal(email, currentUser.Email);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsGenericUnauthorizedProblem()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123",
            rememberMe = false,
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Invalid credentials", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_AfterRepeatedFailures_LocksAccount()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await AddAntiforgeryHeaderAsync(client);
            await client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = "WrongPassword123",
                rememberMe = false,
            });
        }

        var lockedResponse = await LoginAsync(client, email);

        Assert.Equal((HttpStatusCode)423, lockedResponse.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithSession_ReturnsCurrentUser()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await client.GetAsync("/api/auth/me");
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal(email, currentUser.Email);
    }

    [Fact]
    public async Task Logout_InvalidatesSession()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);
        await AddAntiforgeryHeaderAsync(client);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_WithValidAntiforgeryToken_Works()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithInvalidAntiforgeryToken_IsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        await AddAntiforgeryHeaderAsync(client);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid-request-token");

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AntiforgeryToken_ReturnsAngularReadableCookieAndNoStoreHeaders()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/antiforgery/token");
        var body = await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>();
        var requestTokenCookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.RequestToken));
        Assert.Contains("samesite=lax", requestTokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", requestTokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task UnauthenticatedApiRequest_Returns401WithoutHtmlRedirect()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/me");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Location is not null);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task AuthenticationCookie_HasExpectedAttributes()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        var response = await LoginAsync(client, email, rememberMe: true);
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("PersonalOS.Auth=", StringComparison.Ordinal));

        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CurrentUserResponse_DoesNotExposeSensitiveIdentityFields()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = CreateClient(factory);
        var email = NewEmail();

        await RegisterAsync(client, email);
        await LoginAsync(client, email);

        var response = await client.GetAsync("/api/auth/me");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConcurrencyStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccessFailedCount", body, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateClient(PersonalOSWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string? email = null)
    {
        await AddAntiforgeryHeaderAsync(client);

        return await client.PostAsJsonAsync(
            "/api/auth/register",
            NewRegisterRequest(email));
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        bool rememberMe = false)
    {
        await AddAntiforgeryHeaderAsync(client);

        return await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = StrongPassword,
            rememberMe,
        });
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/antiforgery/token");

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestToken));

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", response.RequestToken);
    }

    private static object NewRegisterRequest(string? email = null) => new
    {
        displayName = "Jefferson",
        email = email ?? NewEmail(),
        password = StrongPassword,
    };

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private sealed record AntiforgeryTokenResponse(string RequestToken);

    private sealed record CurrentUserResponse(Guid Id, string DisplayName, string Email);
}
