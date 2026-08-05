using System.Diagnostics.CodeAnalysis;

namespace PersonalOS.Domain.Common;

/// <summary>
/// Normalization and length rules shared by every text field in the daily modules.
/// </summary>
/// <remarks>
/// The rules live in the domain so that entities, application services, and tests apply exactly
/// one definition of "acceptable text" instead of repeating trimming and length checks in every
/// controller. A value made only of whitespace is never acceptable for a required field.
/// </remarks>
public static class TextRules
{
    /// <summary>
    /// Trims a required value and reports whether it fits inside the allowed length.
    /// </summary>
    /// <param name="value">Raw value supplied by the caller.</param>
    /// <param name="minLength">Minimum length after trimming.</param>
    /// <param name="maxLength">Maximum length after trimming.</param>
    /// <param name="normalized">Trimmed value when the candidate is acceptable.</param>
    /// <returns><see langword="true"/> when the trimmed value can be stored.</returns>
    public static bool TryNormalizeRequired(
        string? value,
        int minLength,
        int maxLength,
        [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (value is null)
        {
            return false;
        }

        var trimmed = value.Trim();

        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            return false;
        }

        normalized = trimmed;

        return true;
    }

    /// <summary>
    /// Trims an optional value, turning an empty or whitespace-only value into <see langword="null"/>.
    /// </summary>
    /// <param name="value">Raw value supplied by the caller.</param>
    /// <param name="maxLength">Maximum length after trimming.</param>
    /// <param name="normalized">Trimmed value, or <see langword="null"/> when nothing was supplied.</param>
    /// <returns><see langword="false"/> only when the trimmed value is too long.</returns>
    /// <remarks>
    /// Storing <see langword="null"/> rather than an empty string keeps "the user wrote nothing"
    /// distinguishable from "the user wrote and then erased", and keeps queries simple.
    /// </remarks>
    public static bool TryNormalizeOptional(string? value, int maxLength, out string? normalized)
    {
        normalized = null;

        if (value is null)
        {
            return true;
        }

        var trimmed = value.Trim();

        if (trimmed.Length == 0)
        {
            return true;
        }

        if (trimmed.Length > maxLength)
        {
            return false;
        }

        normalized = trimmed;

        return true;
    }

    /// <summary>
    /// Trims a required value or throws, for use inside domain factory methods.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <param name="minLength">Minimum length after trimming.</param>
    /// <param name="maxLength">Maximum length after trimming.</param>
    /// <param name="parameterName">Name reported by the exception.</param>
    /// <returns>The trimmed value.</returns>
    /// <exception cref="ArgumentException">The value cannot be stored.</exception>
    public static string NormalizeRequiredOrThrow(
        string? value,
        int minLength,
        int maxLength,
        string parameterName)
    {
        if (!TryNormalizeRequired(value, minLength, maxLength, out var normalized))
        {
            throw new ArgumentException(
                $"A value between {minLength} and {maxLength} characters is required.",
                parameterName);
        }

        return normalized;
    }

    /// <summary>
    /// Trims an optional value or throws, for use inside domain factory methods.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <param name="maxLength">Maximum length after trimming.</param>
    /// <param name="parameterName">Name reported by the exception.</param>
    /// <returns>The trimmed value, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">The value is longer than the column allows.</exception>
    public static string? NormalizeOptionalOrThrow(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (!TryNormalizeOptional(value, maxLength, out var normalized))
        {
            throw new ArgumentException(
                $"A value of {maxLength} characters or fewer is required.",
                parameterName);
        }

        return normalized;
    }
}
