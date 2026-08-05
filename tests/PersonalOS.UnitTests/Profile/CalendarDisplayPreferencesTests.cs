using PersonalOS.Application.Common;
using PersonalOS.Application.Profile;
using PersonalOS.Domain.Users;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Profile;

/// <summary>
/// How the day planner's visible hours are validated and saved.
/// </summary>
public sealed class CalendarDisplayPreferencesTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    private readonly InMemoryUserProfileStore store = new();
    private readonly UserProfileService service;

    public CalendarDisplayPreferencesTests()
    {
        service = new UserProfileService(store, new FixedClock(UtcNow));
        store.Seed(UserA, "Jefferson", "a@example.com");
        store.Seed(UserB, "Other", "b@example.com");
    }

    [Fact]
    public void AnAccountThatHasNeverChosenSeesTheDefaults()
    {
        var preferences = UserPreferences.Create(UserA, "UTC", UtcNow);

        Assert.Equal(new TimeOnly(6, 0), preferences.CalendarDayStartTime);
        Assert.Equal(new TimeOnly(22, 0), preferences.CalendarDayEndTime);
        Assert.Equal(15, preferences.CalendarSlotMinutes);
    }

    [Fact]
    public void AValidWindowIsSaved()
    {
        var preferences = UserPreferences.Create(UserA, "UTC", UtcNow);

        preferences.UpdateCalendarDisplay(new TimeOnly(8, 0), new TimeOnly(18, 0), 30, UtcNow);

        Assert.Equal(new TimeOnly(8, 0), preferences.CalendarDayStartTime);
        Assert.Equal(new TimeOnly(18, 0), preferences.CalendarDayEndTime);
        Assert.Equal(30, preferences.CalendarSlotMinutes);
    }

    [Fact]
    public void AWindowThatEndsWhenItStartsIsRefused()
    {
        var preferences = UserPreferences.Create(UserA, "UTC", UtcNow);

        // A range with no hours in it draws a timeline with no rows, which is unusable and almost
        // certainly a typing mistake.
        Assert.False(UserPreferences.IsCalendarRangeValid(new TimeOnly(9, 0), new TimeOnly(9, 0)));
        Assert.Throws<ArgumentException>(() =>
            preferences.UpdateCalendarDisplay(new TimeOnly(9, 0), new TimeOnly(9, 0), 15, UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(45)]
    [InlineData(120)]
    public void AnIntervalTheGridCannotDrawIsRefused(int slotMinutes)
    {
        Assert.False(UserPreferences.IsCalendarSlotValid(slotMinutes));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void EveryOfferedIntervalIsAccepted(int slotMinutes)
    {
        Assert.True(UserPreferences.IsCalendarSlotValid(slotMinutes));
    }

    [Fact]
    public async Task SavingAValidWindowReturnsTheUpdatedProfile()
    {
        var result = await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(7, 0),
            new TimeOnly(19, 0),
            30,
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Saved, result.Status);
        Assert.Equal(new TimeOnly(7, 0), result.Profile!.CalendarDisplay.DayStartTime);
        Assert.Equal(30, result.Profile.CalendarDisplay.SlotMinutes);
    }

    [Fact]
    public async Task AStartAfterTheEndIsRejectedWithAFieldMessage()
    {
        var result = await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            15,
            CancellationToken.None);

        // Rejected rather than quietly swapped: the user should find out their choice was refused
        // instead of silently getting a different one.
        Assert.Equal(UserProfileUpdateStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(UserProfileService.DayEndTimeField));
    }

    [Fact]
    public async Task AnUnofferedIntervalIsRejectedWithAFieldMessage()
    {
        var result = await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(6, 0),
            new TimeOnly(22, 0),
            7,
            CancellationToken.None);

        Assert.Equal(UserProfileUpdateStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(UserProfileService.SlotMinutesField));
    }

    [Fact]
    public async Task ARejectedChangeIsNeverWritten()
    {
        await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            15,
            CancellationToken.None);

        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task OneAccountsWindowDoesNotChangeAnother()
    {
        await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(5, 0),
            new TimeOnly(12, 0),
            60,
            CancellationToken.None);

        var other = await service.GetAsync(UserB, CancellationToken.None);

        Assert.Equal(CalendarDisplayRecord.Default, other!.CalendarDisplay);
    }

    [Fact]
    public async Task SavingTheWindowLeavesTheDisplayNameAndTimeZoneAlone()
    {
        await service.UpdateCalendarDisplayAsync(
            UserA,
            new TimeOnly(5, 0),
            new TimeOnly(12, 0),
            60,
            CancellationToken.None);

        var profile = await service.GetAsync(UserA, CancellationToken.None);

        // The toolbar changes only the timeline. Sending a name and a zone along with it would let
        // the calendar overwrite settings that belong to another screen.
        Assert.Equal("Jefferson", profile!.DisplayName);
        Assert.Equal(UserPreferences.DefaultTimeZoneId, profile.TimeZoneId);
    }
}
