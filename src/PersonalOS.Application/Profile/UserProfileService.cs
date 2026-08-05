using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Time;
using PersonalOS.Domain.Users;

namespace PersonalOS.Application.Profile;

/// <summary>
/// Reads and updates the profile of the authenticated account.
/// </summary>
/// <remarks>
/// Validation lives here rather than in the API controller so that the display-name and
/// time-zone rules can be unit tested without an HTTP host, and so that every caller applies the
/// same rules.
/// </remarks>
public sealed class UserProfileService(IUserProfileStore store, IClock clock)
{
    /// <summary>
    /// Field name used for display-name validation messages.
    /// </summary>
    public const string DisplayNameField = "displayName";

    /// <summary>
    /// Field name used for time-zone validation messages.
    /// </summary>
    public const string TimeZoneIdField = "timeZoneId";

    /// <summary>Field name used for visible-hours validation messages.</summary>
    public const string DayEndTimeField = "dayEndTime";

    /// <summary>Field name used for slot-length validation messages.</summary>
    public const string SlotMinutesField = "slotMinutes";

    /// <summary>
    /// Reads the profile of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<UserProfileRecord?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        store.GetAsync(userId, cancellationToken);

    /// <summary>
    /// Validates and saves the display name and time zone of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="displayName">Raw display name submitted by the client.</param>
    /// <param name="timeZoneId">Raw time-zone identifier submitted by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The email address is intentionally not part of this operation. Changing it would require
    /// confirmation and recovery flows that do not exist yet.
    /// </remarks>
    public async Task<UserProfileUpdateResult> UpdateAsync(
        Guid userId,
        string? displayName,
        string? timeZoneId,
        CancellationToken cancellationToken)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (!DisplayNameRules.TryNormalize(displayName, out var normalizedDisplayName))
        {
            validationErrors[DisplayNameField] = [DisplayNameRules.ValidationMessage];
        }

        if (!TimeZoneCatalog.TryResolve(timeZoneId, out var timeZone))
        {
            validationErrors[TimeZoneIdField] = [TimeZoneCatalog.ValidationMessage];
        }

        if (validationErrors.Count > 0 || normalizedDisplayName is null || timeZone is null)
        {
            return UserProfileUpdateResult.Invalid(validationErrors);
        }

        // Storing the resolved identifier canonicalizes the submitted value.
        var saved = await store.SaveAsync(
            userId,
            normalizedDisplayName,
            timeZone.Id,
            clock.UtcNow,
            cancellationToken);

        return saved is null
            ? UserProfileUpdateResult.AccountNotFound()
            : UserProfileUpdateResult.Saved(saved);
    }

    /// <summary>
    /// Validates and saves how the day planner's timeline is shown to one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="dayStartTime">First visible local time.</param>
    /// <param name="dayEndTime">Last visible local time.</param>
    /// <param name="slotMinutes">How many minutes each slot covers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// An invalid range is rejected with a field message rather than quietly corrected. Swapping the
    /// two values, or snapping them to something legal, would leave the user looking at a timeline
    /// they did not ask for and no explanation of why.
    /// </remarks>
    public async Task<UserProfileUpdateResult> UpdateCalendarDisplayAsync(
        Guid userId,
        TimeOnly dayStartTime,
        TimeOnly dayEndTime,
        int slotMinutes,
        CancellationToken cancellationToken)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (!UserPreferences.IsCalendarRangeValid(dayStartTime, dayEndTime))
        {
            validationErrors[DayEndTimeField] =
                ["The start time must be earlier than the end time."];
        }

        if (!UserPreferences.IsCalendarSlotValid(slotMinutes))
        {
            validationErrors[SlotMinutesField] =
                [$"Choose an interval of {string.Join(", ", UserPreferences.AllowedCalendarSlotMinutes)} minutes."];
        }

        if (validationErrors.Count > 0)
        {
            return UserProfileUpdateResult.Invalid(validationErrors);
        }

        var saved = await store.SaveCalendarDisplayAsync(
            userId,
            new CalendarDisplayRecord(dayStartTime, dayEndTime, slotMinutes),
            clock.UtcNow,
            cancellationToken);

        return saved is null
            ? UserProfileUpdateResult.AccountNotFound()
            : UserProfileUpdateResult.Saved(saved);
    }

    /// <summary>
    /// Gives a newly registered account a valid default preferences record.
    /// </summary>
    /// <param name="userId">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task InitializeAsync(Guid userId, CancellationToken cancellationToken) =>
        store.EnsurePreferencesAsync(userId, clock.UtcNow, cancellationToken);
}
