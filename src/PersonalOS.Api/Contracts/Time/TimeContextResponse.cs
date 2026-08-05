using PersonalOS.Application.Time;

namespace PersonalOS.Api.Contracts.Time;

/// <summary>
/// The current instant expressed for the authenticated account.
/// </summary>
/// <param name="UtcNow">Current instant in UTC.</param>
/// <param name="LocalNow">The same instant carrying the account's local offset.</param>
/// <param name="LocalDate">The account's local calendar date, as <c>yyyy-MM-dd</c>.</param>
/// <param name="TimeZoneId">IANA identifier used for the conversion.</param>
/// <param name="UtcOffsetMinutes">Offset applied at this instant, in minutes.</param>
/// <remarks>
/// All values are machine-readable and are not localized. The client is responsible for
/// presenting them, which keeps the server free of display-language concerns.
/// </remarks>
public sealed record TimeContextResponse(
    DateTimeOffset UtcNow,
    DateTimeOffset LocalNow,
    DateOnly LocalDate,
    string TimeZoneId,
    int UtcOffsetMinutes)
{
    /// <summary>
    /// Projects an application result onto the public contract.
    /// </summary>
    /// <param name="localTime">Application conversion result.</param>
    public static TimeContextResponse FromLocalTime(UserLocalTime localTime) =>
        new(
            localTime.UtcNow,
            localTime.LocalNow,
            localTime.LocalDate,
            localTime.TimeZoneId,
            localTime.UtcOffsetMinutes);
}
