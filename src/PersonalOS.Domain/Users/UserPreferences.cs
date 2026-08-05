namespace PersonalOS.Domain.Users;

/// <summary>
/// Application preferences owned by exactly one account.
/// </summary>
/// <remarks>
/// The domain model stores the IANA time-zone identifier as an opaque string. Resolving that
/// identifier against the host time-zone database belongs to the application layer, because it
/// depends on the runtime environment rather than on a business rule.
/// </remarks>
public sealed class UserPreferences
{
    /// <summary>
    /// Time zone assigned to accounts that have never chosen one.
    /// </summary>
    public const string DefaultTimeZoneId = "UTC";

    /// <summary>
    /// Maximum stored length of an IANA time-zone identifier.
    /// </summary>
    /// <remarks>
    /// The longest identifier published by the IANA time-zone database is well below this value,
    /// so the limit rejects abusive input without rejecting legitimate zones.
    /// </remarks>
    public const int TimeZoneIdMaxLength = 100;

    /// <summary>Minutes each timeline slot covers for an account that has never chosen.</summary>
    public const int DefaultCalendarSlotMinutes = 15;

    /// <summary>First hour the day planner shows for an account that has never chosen.</summary>
    public static readonly TimeOnly DefaultCalendarDayStartTime = new(6, 0);

    /// <summary>Last hour the day planner shows for an account that has never chosen.</summary>
    public static readonly TimeOnly DefaultCalendarDayEndTime = new(22, 0);

    /// <summary>
    /// Slot lengths the day planner offers.
    /// </summary>
    /// <remarks>
    /// The list is closed rather than a range. An arbitrary number of minutes would produce a grid
    /// whose rows do not line up with the hour marks, and nothing about a personal planner needs
    /// seven-minute slots.
    /// </remarks>
    public static readonly IReadOnlyList<int> AllowedCalendarSlotMinutes = [15, 30, 60];

    private UserPreferences()
    {
    }

    /// <summary>
    /// Identifier of the owning account. This is also the primary key, which enforces the
    /// one-preferences-record-per-user invariant in the database.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// IANA time-zone identifier, for example <c>UTC</c> or <c>America/Costa_Rica</c>.
    /// </summary>
    public string TimeZoneId { get; private set; } = DefaultTimeZoneId;

    /// <summary>
    /// First local time of day the planner's timeline shows.
    /// </summary>
    /// <remarks>
    /// The visible window is a display choice, not a rule about when activities may happen. An
    /// activity outside it still exists, still counts, and is still reachable; the planner shows it
    /// in a separate section rather than hiding it.
    /// </remarks>
    public TimeOnly CalendarDayStartTime { get; private set; } = DefaultCalendarDayStartTime;

    /// <summary>Last local time of day the planner's timeline shows.</summary>
    public TimeOnly CalendarDayEndTime { get; private set; } = DefaultCalendarDayEndTime;

    /// <summary>How many minutes each timeline slot covers.</summary>
    public int CalendarSlotMinutes { get; private set; } = DefaultCalendarSlotMinutes;

    /// <summary>
    /// Instant the preferences record was created, in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// Instant the preferences record was last saved, in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates preferences for an account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="timeZoneId">Validated IANA time-zone identifier.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static UserPreferences Create(Guid userId, string timeZoneId, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var preferences = new UserPreferences
        {
            UserId = userId,
            TimeZoneId = NormalizeTimeZoneId(timeZoneId),
            CreatedAtUtc = utcNow.ToUniversalTime(),
        };

        preferences.UpdatedAtUtc = preferences.CreatedAtUtc;

        return preferences;
    }

    /// <summary>
    /// Applies a saved preferences change and records when it happened.
    /// </summary>
    /// <param name="timeZoneId">Validated IANA time-zone identifier.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(string timeZoneId, DateTimeOffset utcNow)
    {
        TimeZoneId = NormalizeTimeZoneId(timeZoneId);
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Reports whether a visible-hours range can be shown.
    /// </summary>
    /// <param name="startTime">First visible local time.</param>
    /// <param name="endTime">Last visible local time.</param>
    /// <remarks>
    /// The comparison is strict. A range that starts when it ends has no rows in it, which is a
    /// timeline the user cannot use and almost certainly a typing mistake.
    /// </remarks>
    public static bool IsCalendarRangeValid(TimeOnly startTime, TimeOnly endTime) =>
        startTime < endTime;

    /// <summary>
    /// Reports whether a slot length is one the planner offers.
    /// </summary>
    /// <param name="slotMinutes">Candidate slot length.</param>
    public static bool IsCalendarSlotValid(int slotMinutes) =>
        AllowedCalendarSlotMinutes.Contains(slotMinutes);

    /// <summary>
    /// Applies a saved change to the day planner's visible hours.
    /// </summary>
    /// <param name="startTime">First visible local time.</param>
    /// <param name="endTime">Last visible local time.</param>
    /// <param name="slotMinutes">How many minutes each slot covers.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <exception cref="ArgumentException">The values cannot produce a usable timeline.</exception>
    public void UpdateCalendarDisplay(
        TimeOnly startTime,
        TimeOnly endTime,
        int slotMinutes,
        DateTimeOffset utcNow)
    {
        if (!IsCalendarRangeValid(startTime, endTime))
        {
            throw new ArgumentException(
                "The start time must be earlier than the end time.",
                nameof(endTime));
        }

        if (!IsCalendarSlotValid(slotMinutes))
        {
            throw new ArgumentException(
                "That slot length is not offered.",
                nameof(slotMinutes));
        }

        CalendarDayStartTime = startTime;
        CalendarDayEndTime = endTime;
        CalendarSlotMinutes = slotMinutes;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    private static string NormalizeTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException("A time-zone identifier is required.", nameof(timeZoneId));
        }

        if (timeZoneId.Length > TimeZoneIdMaxLength)
        {
            throw new ArgumentException(
                $"A time-zone identifier must be {TimeZoneIdMaxLength} characters or fewer.",
                nameof(timeZoneId));
        }

        return timeZoneId;
    }
}
