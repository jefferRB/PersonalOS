namespace PersonalOS.Application.Profile;

/// <summary>
/// Persistence port for profile values owned by an account.
/// </summary>
/// <remarks>
/// Every member takes the account identifier that the API derived from the authenticated
/// principal. Implementations must scope all queries by that identifier and must never accept an
/// account identifier supplied by a client.
/// </remarks>
public interface IUserProfileStore
{
    /// <summary>
    /// Reads the profile of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, or <see langword="null"/> when the account no longer exists.</returns>
    /// <remarks>
    /// Reading must not create a preferences record. An account without one reports the default
    /// time zone instead.
    /// </remarks>
    Task<UserProfileRecord?> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the display name and time zone of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="displayName">Normalized display name.</param>
    /// <param name="timeZoneId">Validated IANA time-zone identifier.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile, or <see langword="null"/> when the account no longer exists.</returns>
    Task<UserProfileRecord?> SaveAsync(
        Guid userId,
        string displayName,
        string timeZoneId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves how the day planner's timeline is shown to one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="display">Validated display values.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile, or <see langword="null"/> when the account no longer exists.</returns>
    /// <remarks>
    /// This is separate from <see cref="SaveAsync"/> because the calendar toolbar changes only the
    /// timeline. Making it send a display name and a time zone as well would let a stray value on
    /// an unrelated screen overwrite them.
    /// </remarks>
    Task<UserProfileRecord?> SaveCalendarDisplayAsync(
        Guid userId,
        CalendarDisplayRecord display,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads only the persisted time zone of one account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted identifier, or the default when no record exists.</returns>
    Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the default preferences record for an account when it does not have one.
    /// </summary>
    /// <param name="userId">Account identifier.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsurePreferencesAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
