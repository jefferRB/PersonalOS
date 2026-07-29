using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PersonalOS.IntegrationTests;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Live_ReturnsHealthy()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ready_ReturnsHealthyWhenDatabaseIsAvailable()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }
}
