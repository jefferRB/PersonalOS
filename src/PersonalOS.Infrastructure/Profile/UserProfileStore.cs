using Microsoft.EntityFrameworkCore;
using PersonalOS.Application.Profile;
using PersonalOS.Domain.Users;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.Infrastructure.Profile;

/// <summary>
/// EF Core implementation of <see cref="IUserProfileStore"/>.
/// </summary>
/// <remarks>
/// Every query is filtered by the account identifier that the API derived from the authenticated
/// principal, so one account can never read or write another account's profile.
/// </remarks>
public sealed class UserProfileStore(ApplicationDbContext dbContext) : IUserProfileStore
{
    /// <inheritdoc />
    public async Task<UserProfileRecord?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.DisplayName,
                user.Email,
                user.CreatedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return null;
        }

        // A read never creates a preferences record. Accounts that predate the preferences table
        // and have not been backfilled report the default time zone instead.
        var preferences = await dbContext.UserPreferences
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new
            {
                item.TimeZoneId,
                item.CalendarDayStartTime,
                item.CalendarDayEndTime,
                item.CalendarSlotMinutes,
                item.UpdatedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new UserProfileRecord(
            account.DisplayName,
            account.Email ?? string.Empty,
            preferences?.TimeZoneId ?? UserPreferences.DefaultTimeZoneId,
            preferences is null
                ? CalendarDisplayRecord.Default
                : new CalendarDisplayRecord(
                    preferences.CalendarDayStartTime,
                    preferences.CalendarDayEndTime,
                    preferences.CalendarSlotMinutes),
            preferences?.UpdatedAtUtc ?? account.CreatedAtUtc);
    }

    /// <inheritdoc />
    public async Task<UserProfileRecord?> SaveAsync(
        Guid userId,
        string displayName,
        string timeZoneId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        account.DisplayName = displayName;

        var preferences = await dbContext.UserPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (preferences is null)
        {
            preferences = UserPreferences.Create(userId, timeZoneId, utcNow);
            dbContext.UserPreferences.Add(preferences);
        }
        else
        {
            preferences.Update(timeZoneId, utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Project(account.DisplayName, account.Email, preferences);
    }

    /// <inheritdoc />
    public async Task<UserProfileRecord?> SaveCalendarDisplayAsync(
        Guid userId,
        CalendarDisplayRecord display,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(display);

        var account = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        var preferences = await dbContext.UserPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (preferences is null)
        {
            // An account that predates the preferences table gets its record here rather than
            // losing the setting it just chose.
            preferences = UserPreferences.Create(
                userId,
                UserPreferences.DefaultTimeZoneId,
                utcNow);
            dbContext.UserPreferences.Add(preferences);
        }

        preferences.UpdateCalendarDisplay(
            display.DayStartTime,
            display.DayEndTime,
            display.SlotMinutes,
            utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Project(account.DisplayName, account.Email, preferences);
    }

    private static UserProfileRecord Project(
        string displayName,
        string? email,
        UserPreferences preferences) =>
        new(
            displayName,
            email ?? string.Empty,
            preferences.TimeZoneId,
            new CalendarDisplayRecord(
                preferences.CalendarDayStartTime,
                preferences.CalendarDayEndTime,
                preferences.CalendarSlotMinutes),
            preferences.UpdatedAtUtc);

    /// <inheritdoc />
    public async Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var timeZoneId = await dbContext.UserPreferences
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        return timeZoneId ?? UserPreferences.DefaultTimeZoneId;
    }

    /// <inheritdoc />
    public async Task EnsurePreferencesAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.UserPreferences
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.UserPreferences.Add(
            UserPreferences.Create(userId, UserPreferences.DefaultTimeZoneId, utcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
