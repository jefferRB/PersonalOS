using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Profile;

namespace PersonalOS.Application.Time;

/// <summary>
/// Produces the current time context of the authenticated account.
/// </summary>
public sealed class TimeContextService(
    IClock clock,
    IUserProfileStore store,
    LocalTimeService localTimeService)
{
    /// <summary>
    /// Combines the application clock with the account's persisted time zone.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserLocalTime> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var timeZoneId = await store.GetTimeZoneIdAsync(userId, cancellationToken);

        return localTimeService.Resolve(clock.UtcNow, timeZoneId);
    }
}
