using PersonalOS.Application.Time;
using PersonalOS.UnitTests.Profile;

namespace PersonalOS.UnitTests.Time;

public sealed class TimeContextServiceTests
{
    private static readonly DateTimeOffset FixedInstant =
        new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    private readonly InMemoryUserProfileStore store = new();

    [Fact]
    public async Task GetAsync_UsesThePersistedTimeZoneOfTheAccount()
    {
        var userId = Guid.NewGuid();
        store.Seed(userId, "Jefferson", "jefferson@example.com", "America/Costa_Rica");

        var result = await CreateService().GetAsync(userId, CancellationToken.None);

        Assert.Equal("America/Costa_Rica", result.TimeZoneId);
        Assert.Equal(-360, result.UtcOffsetMinutes);
        Assert.Equal(FixedInstant, result.UtcNow);
        Assert.Equal(new DateOnly(2026, 7, 30), result.LocalDate);
        Assert.Equal(13, result.LocalNow.Hour);
    }

    [Fact]
    public async Task GetAsync_ForAnAccountWithoutPreferences_FallsBackToUtc()
    {
        var result = await CreateService().GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("UTC", result.TimeZoneId);
        Assert.Equal(0, result.UtcOffsetMinutes);
    }

    [Fact]
    public async Task GetAsync_ForTwoAccounts_ProducesIndependentLocalDates()
    {
        var costaRicaUserId = Guid.NewGuid();
        var tokyoUserId = Guid.NewGuid();
        store.Seed(costaRicaUserId, "Jefferson", "a@example.com", "America/Costa_Rica");
        store.Seed(tokyoUserId, "Other Person", "b@example.com", "Asia/Tokyo");

        // 23:30 UTC is still 30 July in Costa Rica but already 31 July in Tokyo.
        var service = CreateService(new DateTimeOffset(2026, 7, 30, 23, 30, 0, TimeSpan.Zero));

        var costaRica = await service.GetAsync(costaRicaUserId, CancellationToken.None);
        var tokyo = await service.GetAsync(tokyoUserId, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 7, 30), costaRica.LocalDate);
        Assert.Equal(new DateOnly(2026, 7, 31), tokyo.LocalDate);
    }

    private TimeContextService CreateService(DateTimeOffset? utcNow = null) =>
        new(new FixedClock(utcNow ?? FixedInstant), store, new LocalTimeService());
}
