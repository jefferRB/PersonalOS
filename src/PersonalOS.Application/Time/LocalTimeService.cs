namespace PersonalOS.Application.Time;

/// <summary>
/// Converts a UTC instant into one account's local time and local calendar date.
/// </summary>
public sealed class LocalTimeService
{
    /// <summary>
    /// Converts a UTC instant using the account's persisted time zone.
    /// </summary>
    /// <param name="utcNow">Instant supplied by <see cref="Abstractions.IClock"/>.</param>
    /// <param name="timeZoneId">Persisted IANA identifier for the account.</param>
    /// <returns>The instant expressed in UTC and in the account's local time.</returns>
    /// <remarks>
    /// An identifier that cannot be resolved on this host falls back to UTC instead of failing.
    /// A stored zone can disappear when the host time-zone database changes, and refusing to
    /// display a date would be a worse outcome than displaying the UTC date.
    /// </remarks>
    public UserLocalTime Resolve(DateTimeOffset utcNow, string? timeZoneId)
    {
        var timeZone = TimeZoneCatalog.TryResolve(timeZoneId, out var resolved)
            ? resolved
            : TimeZoneInfo.Utc;

        var utcInstant = utcNow.ToUniversalTime();
        var localNow = TimeZoneInfo.ConvertTime(utcInstant, timeZone);

        return new UserLocalTime(
            utcInstant,
            localNow,
            DateOnly.FromDateTime(localNow.DateTime),
            timeZone.Id,
            (int)localNow.Offset.TotalMinutes);
    }
}
