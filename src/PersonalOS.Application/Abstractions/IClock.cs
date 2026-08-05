namespace PersonalOS.Application.Abstractions;

/// <summary>
/// Supplies the current instant to application code.
/// </summary>
/// <remarks>
/// Time-dependent application code depends on this abstraction instead of calling
/// <c>DateTime.Now</c> or <c>DateTimeOffset.Now</c>, so tests can run against a fixed instant
/// and remain independent of the machine clock and the machine time zone.
/// </remarks>
public interface IClock
{
    /// <summary>
    /// Current instant in UTC. UTC is the internal source of truth for PersonalOS.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
