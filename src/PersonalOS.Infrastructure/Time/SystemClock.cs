using PersonalOS.Application.Abstractions;

namespace PersonalOS.Infrastructure.Time;

/// <summary>
/// Production <see cref="IClock"/> backed by the host clock.
/// </summary>
/// <remarks>
/// The implementation delegates to <see cref="TimeProvider"/>, which the API already registers,
/// so PersonalOS keeps a single source of time instead of two competing clocks.
/// </remarks>
public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
