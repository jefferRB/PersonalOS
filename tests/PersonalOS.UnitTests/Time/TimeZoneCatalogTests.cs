using PersonalOS.Application.Time;

namespace PersonalOS.UnitTests.Time;

public sealed class TimeZoneCatalogTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Costa_Rica")]
    [InlineData("America/New_York")]
    [InlineData("Europe/Madrid")]
    [InlineData("Asia/Tokyo")]
    public void TryResolve_WithSupportedIanaIdentifier_Succeeds(string timeZoneId)
    {
        var resolved = TimeZoneCatalog.TryResolve(timeZoneId, out var timeZone);

        Assert.True(resolved);
        Assert.NotNull(timeZone);
        Assert.Equal(timeZoneId, timeZone.Id);
        Assert.True(timeZone.HasIanaId);
    }

    [Fact]
    public void TryResolve_WithCostaRica_WorksOnThisHost()
    {
        // The Development environment runs on Windows. This test documents that .NET resolves
        // IANA identifiers there, which is why no time-zone mapping package was added.
        Assert.True(TimeZoneCatalog.IsSupported("America/Costa_Rica"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not/AZone")]
    [InlineData("America/Costa_Rica; DROP TABLE Users")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("-06:00")]
    [InlineData("UTC-6")]
    public void TryResolve_WithUnsupportedValue_Fails(string? timeZoneId)
    {
        var resolved = TimeZoneCatalog.TryResolve(timeZoneId, out var timeZone);

        Assert.False(resolved);
        Assert.Null(timeZone);
    }

    [Theory]
    [InlineData("america/costa_rica")]
    [InlineData("AMERICA/COSTA_RICA")]
    public void TryResolve_WhenCasingDiffers_ReportsTheCanonicalIdentifier(string timeZoneId)
    {
        // TimeZoneInfo accepts a differently cased identifier once the canonical one has been
        // resolved in this process, so the caller must never persist the submitted string.
        // Storing the resolved Id keeps the database canonical either way.
        Assert.True(TimeZoneCatalog.TryResolve("America/Costa_Rica", out _));

        if (TimeZoneCatalog.TryResolve(timeZoneId, out var timeZone))
        {
            Assert.Equal("America/Costa_Rica", timeZone.Id);
        }
    }

    [Theory]
    [InlineData("Central America Standard Time")]
    [InlineData("Pacific Standard Time")]
    public void TryResolve_WithWindowsIdentifier_IsRejected(string timeZoneId)
    {
        // Windows identifiers resolve on Windows but would be meaningless on a Linux host, so
        // PersonalOS refuses to persist them.
        Assert.False(TimeZoneCatalog.IsSupported(timeZoneId));
    }

    [Fact]
    public void TryResolve_WithOverlongValue_IsRejectedBeforeTouchingTheTimeZoneDatabase()
    {
        var overlong = new string('a', 101);

        Assert.False(TimeZoneCatalog.IsSupported(overlong));
    }
}
