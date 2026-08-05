using PersonalOS.Application.Time;

namespace PersonalOS.UnitTests.Time;

public sealed class LocalTimeServiceTests
{
    private readonly LocalTimeService localTimeService = new();

    [Fact]
    public void Resolve_WithUtc_KeepsTheSameWallClock()
    {
        var utcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

        var result = localTimeService.Resolve(utcNow, "UTC");

        Assert.Equal("UTC", result.TimeZoneId);
        Assert.Equal(0, result.UtcOffsetMinutes);
        Assert.Equal(utcNow, result.LocalNow);
        Assert.Equal(new DateOnly(2026, 7, 30), result.LocalDate);
    }

    [Fact]
    public void Resolve_WithCostaRica_AppliesTheFixedMinusSixOffset()
    {
        var utcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

        var result = localTimeService.Resolve(utcNow, "America/Costa_Rica");

        Assert.Equal("America/Costa_Rica", result.TimeZoneId);
        Assert.Equal(-360, result.UtcOffsetMinutes);
        Assert.Equal(new DateTimeOffset(2026, 7, 30, 13, 24, 0, TimeSpan.FromHours(-6)), result.LocalNow);
        Assert.Equal(new DateOnly(2026, 7, 30), result.LocalDate);
    }

    [Fact]
    public void Resolve_WithCostaRica_DoesNotChangeOffsetAcrossTheYear()
    {
        // Costa Rica does not observe daylight saving time.
        var january = localTimeService.Resolve(
            new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            "America/Costa_Rica");
        var july = localTimeService.Resolve(
            new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
            "America/Costa_Rica");

        Assert.Equal(-360, january.UtcOffsetMinutes);
        Assert.Equal(-360, july.UtcOffsetMinutes);
    }

    [Fact]
    public void Resolve_WithDaylightSavingZone_UsesTheOffsetInEffectAtThatInstant()
    {
        // New York is UTC-5 in winter and UTC-4 during daylight saving time. This is why a fixed
        // UTC offset can never replace a time-zone identifier.
        var winter = localTimeService.Resolve(
            new DateTimeOffset(2026, 1, 15, 17, 0, 0, TimeSpan.Zero),
            "America/New_York");
        var summer = localTimeService.Resolve(
            new DateTimeOffset(2026, 7, 15, 17, 0, 0, TimeSpan.Zero),
            "America/New_York");

        Assert.Equal(-300, winter.UtcOffsetMinutes);
        Assert.Equal(12, winter.LocalNow.Hour);

        Assert.Equal(-240, summer.UtcOffsetMinutes);
        Assert.Equal(13, summer.LocalNow.Hour);
    }

    [Fact]
    public void Resolve_WithSouthernHemisphereZone_ReversesTheDaylightSavingDirection()
    {
        var january = localTimeService.Resolve(
            new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            "Australia/Sydney");
        var july = localTimeService.Resolve(
            new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
            "Australia/Sydney");

        Assert.Equal(660, january.UtcOffsetMinutes);
        Assert.Equal(600, july.UtcOffsetMinutes);
    }

    [Fact]
    public void Resolve_JustAfterUtcMidnight_ReturnsThePreviousLocalDateInCostaRica()
    {
        // 00:30 UTC on 31 July is still 18:30 on 30 July in Costa Rica.
        var utcNow = new DateTimeOffset(2026, 7, 31, 0, 30, 0, TimeSpan.Zero);

        var result = localTimeService.Resolve(utcNow, "America/Costa_Rica");

        Assert.Equal(new DateOnly(2026, 7, 30), result.LocalDate);
        Assert.Equal(new DateOnly(2026, 7, 31), DateOnly.FromDateTime(result.UtcNow.UtcDateTime));
    }

    [Fact]
    public void Resolve_JustBeforeUtcMidnight_ReturnsTheNextLocalDateInTokyo()
    {
        // 23:30 UTC on 30 July is already 08:30 on 31 July in Tokyo.
        var utcNow = new DateTimeOffset(2026, 7, 30, 23, 30, 0, TimeSpan.Zero);

        var result = localTimeService.Resolve(utcNow, "Asia/Tokyo");

        Assert.Equal(new DateOnly(2026, 7, 31), result.LocalDate);
        Assert.Equal(540, result.UtcOffsetMinutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not/AZone")]
    [InlineData("America/Costa_Rica; DROP TABLE Users")]
    [InlineData("Central America Standard Time")]
    public void Resolve_WithUnusableIdentifier_FallsBackToUtcInsteadOfFailing(string? timeZoneId)
    {
        var utcNow = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

        var result = localTimeService.Resolve(utcNow, timeZoneId);

        Assert.Equal("UTC", result.TimeZoneId);
        Assert.Equal(0, result.UtcOffsetMinutes);
        Assert.Equal(new DateOnly(2026, 7, 30), result.LocalDate);
    }

    [Fact]
    public void Resolve_NormalizesANonUtcInputInstantBeforeConverting()
    {
        // The same instant expressed with a non-zero offset must produce the same local result.
        var utcInstant = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);
        var shiftedInstant = utcInstant.ToOffset(TimeSpan.FromHours(3));

        var fromUtc = localTimeService.Resolve(utcInstant, "America/Costa_Rica");
        var fromShifted = localTimeService.Resolve(shiftedInstant, "America/Costa_Rica");

        Assert.Equal(fromUtc, fromShifted);
        Assert.Equal(TimeSpan.Zero, fromShifted.UtcNow.Offset);
    }
}
