using PersonalOS.Application.Abstractions;

namespace PersonalOS.UnitTests.Time;

/// <summary>
/// Test clock that always reports the same instant.
/// </summary>
/// <remarks>
/// Time-dependent tests use this instead of the host clock so that results never depend on when
/// or where the suite runs.
/// </remarks>
public sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; } = utcNow;
}
