using System.Diagnostics.CodeAnalysis;
using PersonalOS.Domain.Users;

namespace PersonalOS.Application.Time;

/// <summary>
/// Resolves and validates IANA time-zone identifiers against the host time-zone database.
/// </summary>
/// <remarks>
/// <para>
/// .NET 10 resolves IANA identifiers such as <c>America/Costa_Rica</c> on Windows and on Linux,
/// so no third-party time-zone mapping package is required.
/// </para>
/// <para>
/// Windows-only identifiers such as <c>Central America Standard Time</c> also resolve on Windows,
/// but they are rejected here: storing one would make the persisted value meaningless on a Linux
/// host. <see cref="TimeZoneInfo.HasIanaId"/> is the portable way to tell the two apart.
/// </para>
/// </remarks>
public static class TimeZoneCatalog
{
    /// <summary>
    /// Message returned when a submitted time-zone identifier is rejected.
    /// </summary>
    public const string ValidationMessage =
        "Select a supported IANA time zone, for example UTC or America/Costa_Rica.";

    /// <summary>
    /// Attempts to resolve a submitted identifier to a supported IANA time zone.
    /// </summary>
    /// <param name="timeZoneId">Identifier supplied by the caller.</param>
    /// <param name="timeZone">Resolved time zone when the identifier is supported.</param>
    /// <returns><see langword="true"/> when the identifier is a supported IANA identifier.</returns>
    public static bool TryResolve(string? timeZoneId, [NotNullWhen(true)] out TimeZoneInfo? timeZone)
    {
        timeZone = null;

        if (string.IsNullOrWhiteSpace(timeZoneId)
            || timeZoneId.Length > UserPreferences.TimeZoneIdMaxLength)
        {
            return false;
        }

        TimeZoneInfo resolved;

        try
        {
            resolved = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        if (!resolved.HasIanaId)
        {
            return false;
        }

        timeZone = resolved;

        return true;
    }

    /// <summary>
    /// Reports whether an identifier is a supported IANA time zone.
    /// </summary>
    /// <param name="timeZoneId">Identifier supplied by the caller.</param>
    public static bool IsSupported(string? timeZoneId) => TryResolve(timeZoneId, out _);
}
