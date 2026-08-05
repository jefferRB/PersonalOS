using PersonalOS.Application.Profile;
using PersonalOS.Domain.Users;

namespace PersonalOS.UnitTests.Profile;

/// <summary>
/// In-memory <see cref="IUserProfileStore"/> used to test application rules without a database.
/// </summary>
public sealed class InMemoryUserProfileStore : IUserProfileStore
{
    private readonly Dictionary<Guid, UserProfileRecord> profiles = [];

    /// <summary>
    /// Number of times <see cref="SaveAsync"/> was called.
    /// </summary>
    public int SaveCount { get; private set; }

    /// <summary>
    /// Seeds an account.
    /// </summary>
    public void Seed(Guid userId, string displayName, string email, string? timeZoneId = null) =>
        profiles[userId] = new UserProfileRecord(
            displayName,
            email,
            timeZoneId ?? UserPreferences.DefaultTimeZoneId,
            CalendarDisplayRecord.Default,
            DateTimeOffset.UnixEpoch);

    /// <inheritdoc />
    public Task<UserProfileRecord?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(profiles.GetValueOrDefault(userId));

    /// <inheritdoc />
    public Task<UserProfileRecord?> SaveAsync(
        Guid userId,
        string displayName,
        string timeZoneId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        SaveCount++;

        if (!profiles.TryGetValue(userId, out var existing))
        {
            return Task.FromResult<UserProfileRecord?>(null);
        }

        var saved = existing with
        {
            DisplayName = displayName,
            TimeZoneId = timeZoneId,
            UpdatedAtUtc = utcNow,
        };
        profiles[userId] = saved;

        return Task.FromResult<UserProfileRecord?>(saved);
    }

    /// <inheritdoc />
    public Task<UserProfileRecord?> SaveCalendarDisplayAsync(
        Guid userId,
        CalendarDisplayRecord display,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        SaveCount++;

        if (!profiles.TryGetValue(userId, out var existing))
        {
            return Task.FromResult<UserProfileRecord?>(null);
        }

        var saved = existing with { CalendarDisplay = display, UpdatedAtUtc = utcNow };
        profiles[userId] = saved;

        return Task.FromResult<UserProfileRecord?>(saved);
    }

    /// <inheritdoc />
    public Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(
            profiles.TryGetValue(userId, out var profile)
                ? profile.TimeZoneId
                : UserPreferences.DefaultTimeZoneId);

    /// <inheritdoc />
    public Task EnsurePreferencesAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (!profiles.ContainsKey(userId))
        {
            profiles[userId] = new UserProfileRecord(
                string.Empty,
                string.Empty,
                UserPreferences.DefaultTimeZoneId,
                CalendarDisplayRecord.Default,
                utcNow);
        }

        return Task.CompletedTask;
    }
}
