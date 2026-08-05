using PersonalOS.Application.Profile;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Profile;

public sealed class UserProfileServiceTests
{
    private static readonly DateTimeOffset FixedInstant =
        new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    private static readonly Guid UserId = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");

    private readonly InMemoryUserProfileStore store = new();
    private readonly UserProfileService profileService;

    public UserProfileServiceTests()
    {
        profileService = new UserProfileService(store, new FixedClock(FixedInstant));
        store.Seed(UserId, "Jefferson", "jefferson@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WithValidValues_SavesTrimmedNameAndCanonicalTimeZone()
    {
        var result = await profileService.UpdateAsync(
            UserId,
            "  Jefferson Rojas  ",
            "America/Costa_Rica",
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Saved, result.Status);
        Assert.NotNull(result.Profile);
        Assert.Equal("Jefferson Rojas", result.Profile.DisplayName);
        Assert.Equal("America/Costa_Rica", result.Profile.TimeZoneId);
        Assert.Equal(FixedInstant, result.Profile.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_UsesTheInjectedClockRatherThanTheHostClock()
    {
        var result = await profileService.UpdateAsync(
            UserId,
            "Jefferson",
            "UTC",
            CancellationToken.None);

        Assert.Equal(FixedInstant, result.Profile!.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_NeverChangesTheEmailAddress()
    {
        var result = await profileService.UpdateAsync(
            UserId,
            "Renamed",
            "Europe/Madrid",
            CancellationToken.None);

        Assert.Equal("jefferson@example.com", result.Profile!.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("J")]
    public async Task UpdateAsync_WithUnacceptableDisplayName_ReportsTheDisplayNameField(
        string? displayName)
    {
        var result = await profileService.UpdateAsync(
            UserId,
            displayName,
            "UTC",
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(UserProfileService.DisplayNameField));
        Assert.False(result.ValidationErrors.ContainsKey(UserProfileService.TimeZoneIdField));
        Assert.Equal(0, store.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    [InlineData("Central America Standard Time")]
    [InlineData("-06:00")]
    public async Task UpdateAsync_WithUnsupportedTimeZone_ReportsTheTimeZoneField(
        string? timeZoneId)
    {
        var result = await profileService.UpdateAsync(
            UserId,
            "Jefferson",
            timeZoneId,
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(UserProfileService.TimeZoneIdField));
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_WithTwoInvalidFields_ReportsBoth()
    {
        var result = await profileService.UpdateAsync(
            UserId,
            "   ",
            "Not/AZone",
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Invalid, result.Status);
        Assert.Equal(2, result.ValidationErrors.Count);
    }

    [Fact]
    public async Task UpdateAsync_ForAnUnknownAccount_ReportsAccountNotFound()
    {
        var result = await profileService.UpdateAsync(
            Guid.NewGuid(),
            "Jefferson",
            "UTC",
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.AccountNotFound, result.Status);
        Assert.Null(result.Profile);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAffectAnotherAccount()
    {
        var otherUserId = Guid.NewGuid();
        store.Seed(otherUserId, "Other Person", "other@example.com", "Asia/Tokyo");

        await profileService.UpdateAsync(
            UserId,
            "Jefferson Rojas",
            "America/Costa_Rica",
            CancellationToken.None);

        var other = await profileService.GetAsync(otherUserId, CancellationToken.None);

        Assert.Equal("Other Person", other!.DisplayName);
        Assert.Equal("Asia/Tokyo", other.TimeZoneId);
    }

    [Fact]
    public async Task InitializeAsync_GivesANewAccountTheDefaultTimeZone()
    {
        var newUserId = Guid.NewGuid();

        await profileService.InitializeAsync(newUserId, CancellationToken.None);

        var timeZoneId = await store.GetTimeZoneIdAsync(newUserId, CancellationToken.None);

        Assert.Equal("UTC", timeZoneId);
    }
}
