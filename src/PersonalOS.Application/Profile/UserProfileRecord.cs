using PersonalOS.Domain.Users;

namespace PersonalOS.Application.Profile;

/// <summary>
/// How the day planner's timeline is shown to one account.
/// </summary>
/// <param name="DayStartTime">First visible local time.</param>
/// <param name="DayEndTime">Last visible local time.</param>
/// <param name="SlotMinutes">How many minutes each slot covers.</param>
/// <remarks>
/// These are display choices, not rules about when activities may happen. An activity outside the
/// visible window still exists and is still reachable from the planner.
/// </remarks>
public sealed record CalendarDisplayRecord(
    TimeOnly DayStartTime,
    TimeOnly DayEndTime,
    int SlotMinutes)
{
    /// <summary>The values an account that has never chosen sees.</summary>
    public static CalendarDisplayRecord Default { get; } = new(
        UserPreferences.DefaultCalendarDayStartTime,
        UserPreferences.DefaultCalendarDayEndTime,
        UserPreferences.DefaultCalendarSlotMinutes);
}

/// <summary>
/// Profile values that belong to one authenticated account.
/// </summary>
/// <param name="DisplayName">Name shown in the application shell and greetings.</param>
/// <param name="Email">Sign-in address. Milestone 2 exposes it as read-only.</param>
/// <param name="TimeZoneId">Persisted IANA time-zone identifier.</param>
/// <param name="CalendarDisplay">How the day planner's timeline is shown.</param>
/// <param name="UpdatedAtUtc">Instant the preferences were last saved, in UTC.</param>
/// <remarks>
/// This record deliberately excludes password hashes, security stamps, concurrency stamps,
/// lockout counters, claims, and every other ASP.NET Core Identity field.
/// </remarks>
public sealed record UserProfileRecord(
    string DisplayName,
    string Email,
    string TimeZoneId,
    CalendarDisplayRecord CalendarDisplay,
    DateTimeOffset UpdatedAtUtc);
