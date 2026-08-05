using PersonalOS.Application.Profile;

namespace PersonalOS.Api.Contracts.Profile;

/// <summary>
/// How the day planner's timeline is shown to the authenticated account.
/// </summary>
/// <param name="DayStartTime">First visible local time, as <c>HH:mm</c>.</param>
/// <param name="DayEndTime">Last visible local time, as <c>HH:mm</c>.</param>
/// <param name="SlotMinutes">How many minutes each slot covers.</param>
public sealed record CalendarDisplayResponse(
    TimeOnly DayStartTime,
    TimeOnly DayEndTime,
    int SlotMinutes)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application record.</param>
    public static CalendarDisplayResponse FromRecord(CalendarDisplayRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CalendarDisplayResponse(
            record.DayStartTime,
            record.DayEndTime,
            record.SlotMinutes);
    }
}

/// <summary>
/// Profile of the authenticated account.
/// </summary>
/// <param name="DisplayName">Name shown in the application shell and greetings.</param>
/// <param name="Email">Sign-in address. Read-only in Milestone 2.</param>
/// <param name="TimeZoneId">Persisted IANA time-zone identifier.</param>
/// <param name="CalendarDisplay">How the day planner's timeline is shown.</param>
/// <param name="UpdatedAtUtc">Instant the preferences were last saved, in UTC.</param>
public sealed record UserProfileResponse(
    string DisplayName,
    string Email,
    string TimeZoneId,
    CalendarDisplayResponse CalendarDisplay,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>
    /// Projects an application record onto the public contract.
    /// </summary>
    /// <param name="record">Application profile record.</param>
    public static UserProfileResponse FromRecord(UserProfileRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new UserProfileResponse(
            record.DisplayName,
            record.Email,
            record.TimeZoneId,
            CalendarDisplayResponse.FromRecord(record.CalendarDisplay),
            record.UpdatedAtUtc);
    }
}

/// <summary>
/// Values a client may send to change how the day planner's timeline is shown.
/// </summary>
/// <remarks>
/// The contract carries no account identifier. The caller already proved who it is with the
/// authentication cookie, and it carries no display name or time zone either, so a change to the
/// timeline cannot overwrite settings that belong to another screen.
/// </remarks>
public sealed class UpdateCalendarDisplayRequest
{
    /// <summary>First visible local time, as <c>HH:mm</c>.</summary>
    public TimeOnly DayStartTime { get; init; }

    /// <summary>Last visible local time, as <c>HH:mm</c>.</summary>
    public TimeOnly DayEndTime { get; init; }

    /// <summary>How many minutes each slot covers.</summary>
    public int SlotMinutes { get; init; }
}
