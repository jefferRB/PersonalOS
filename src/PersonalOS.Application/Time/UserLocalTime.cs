namespace PersonalOS.Application.Time;

/// <summary>
/// The same instant expressed in UTC and in one account's local time zone.
/// </summary>
/// <param name="UtcNow">Current instant in UTC.</param>
/// <param name="LocalNow">The same instant carrying the account's local offset.</param>
/// <param name="LocalDate">The account's local calendar date.</param>
/// <param name="TimeZoneId">IANA identifier actually used for the conversion.</param>
/// <param name="UtcOffsetMinutes">
/// Offset applied at this instant. The offset is a result of the conversion, never the stored
/// preference, because a zone's offset changes across daylight-saving transitions.
/// </param>
public sealed record UserLocalTime(
    DateTimeOffset UtcNow,
    DateTimeOffset LocalNow,
    DateOnly LocalDate,
    string TimeZoneId,
    int UtcOffsetMinutes);
