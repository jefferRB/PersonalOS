using PersonalOS.Application.Abstractions;

namespace PersonalOS.UnitTests.Time;

public sealed class FixedClockTests
{
    [Fact]
    public void UtcNow_ReturnsTheConfiguredInstant()
    {
        var instant = new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

        IClock clock = new FixedClock(instant);

        Assert.Equal(instant, clock.UtcNow);
    }

    [Fact]
    public void UtcNow_DoesNotAdvanceBetweenReads()
    {
        IClock clock = new FixedClock(new DateTimeOffset(2026, 7, 30, 19, 24, 0, TimeSpan.Zero));

        var first = clock.UtcNow;
        var second = clock.UtcNow;

        Assert.Equal(first, second);
    }
}
