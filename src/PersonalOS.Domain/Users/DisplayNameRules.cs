using System.Diagnostics.CodeAnalysis;

namespace PersonalOS.Domain.Users;

/// <summary>
/// Rules that decide whether a display name is acceptable.
/// </summary>
/// <remarks>
/// The rules live in the domain so that registration, profile updates, and tests share one
/// definition instead of duplicating the check inside controllers.
/// </remarks>
public static class DisplayNameRules
{
    /// <summary>
    /// Minimum length after trimming surrounding whitespace.
    /// </summary>
    public const int MinLength = 2;

    /// <summary>
    /// Maximum length after trimming surrounding whitespace. This matches the persisted column.
    /// </summary>
    public const int MaxLength = 100;

    /// <summary>
    /// Message returned when a display name is rejected.
    /// </summary>
    public const string ValidationMessage =
        "Display name must be between 2 and 100 characters and cannot be only whitespace.";

    /// <summary>
    /// Trims the candidate value and reports whether it satisfies the display-name rules.
    /// </summary>
    /// <param name="value">Raw value supplied by the caller.</param>
    /// <param name="normalized">Trimmed value when the candidate is acceptable.</param>
    /// <returns><see langword="true"/> when the trimmed value can be stored.</returns>
    public static bool TryNormalize(string? value, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (value is null)
        {
            return false;
        }

        var trimmed = value.Trim();

        if (trimmed.Length is < MinLength or > MaxLength)
        {
            return false;
        }

        normalized = trimmed;

        return true;
    }
}
