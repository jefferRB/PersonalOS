namespace PersonalOS.Application.Profile;

/// <summary>
/// Outcome of a profile update attempt.
/// </summary>
public sealed class UserProfileUpdateResult
{
    private UserProfileUpdateResult(
        UserProfileUpdateStatus status,
        UserProfileRecord? profile,
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        Status = status;
        Profile = profile;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// What happened during the update.
    /// </summary>
    public UserProfileUpdateStatus Status { get; }

    /// <summary>
    /// Saved profile when <see cref="Status"/> is <see cref="UserProfileUpdateStatus.Saved"/>.
    /// </summary>
    public UserProfileRecord? Profile { get; }

    /// <summary>
    /// Field-level validation messages keyed by the camel-case contract field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="profile">Saved profile.</param>
    public static UserProfileUpdateResult Saved(UserProfileRecord profile) =>
        new(UserProfileUpdateStatus.Saved, profile, EmptyErrors);

    /// <summary>
    /// Creates a rejected result carrying field-level messages.
    /// </summary>
    /// <param name="validationErrors">Messages keyed by contract field name.</param>
    public static UserProfileUpdateResult Invalid(
        IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(UserProfileUpdateStatus.Invalid, profile: null, validationErrors);

    /// <summary>
    /// Creates a result for an account that no longer exists.
    /// </summary>
    public static UserProfileUpdateResult AccountNotFound() =>
        new(UserProfileUpdateStatus.AccountNotFound, profile: null, EmptyErrors);

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>();
}

/// <summary>
/// Possible outcomes of a profile update attempt.
/// </summary>
public enum UserProfileUpdateStatus
{
    /// <summary>The profile was saved.</summary>
    Saved,

    /// <summary>The submitted values were rejected.</summary>
    Invalid,

    /// <summary>The authenticated account no longer exists.</summary>
    AccountNotFound,
}
