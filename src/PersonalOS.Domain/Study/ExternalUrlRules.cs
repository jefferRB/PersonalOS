using System.Diagnostics.CodeAnalysis;

namespace PersonalOS.Domain.Study;

/// <summary>
/// Rules that decide whether a study resource link may be stored.
/// </summary>
/// <remarks>
/// <para>
/// A link the user saves is later rendered as an anchor. Only absolute <c>http</c> and
/// <c>https</c> URLs are accepted, which is what stops <c>javascript:</c>, <c>data:</c>,
/// <c>vbscript:</c>, and <c>file:</c> values from ever reaching a template. Rejecting them at the
/// point of storage means the check cannot be forgotten later by a new screen.
/// </para>
/// <para>
/// The link is metadata only. PersonalOS never requests the address from the server, never
/// renders anything the address returns, and never previews it. A stored URL is a string the user
/// can click, not content the application trusts.
/// </para>
/// </remarks>
public static class ExternalUrlRules
{
    /// <summary>Maximum stored length of a resource URL.</summary>
    public const int MaxLength = 2000;

    /// <summary>Message returned when a submitted URL is rejected.</summary>
    public const string ValidationMessage =
        "Enter a complete link that starts with http:// or https://.";

    /// <summary>
    /// Trims an optional URL and reports whether it is a safe absolute web address.
    /// </summary>
    /// <param name="value">Raw value supplied by the caller.</param>
    /// <param name="normalized">
    /// The trimmed URL, or <see langword="null"/> when no URL was supplied.
    /// </param>
    /// <returns><see langword="true"/> when the value is empty or a usable web address.</returns>
    public static bool TryNormalize(string? value, out string? normalized)
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

        if (trimmed.Length > MaxLength)
        {
            return false;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        normalized = trimmed;

        return true;
    }

    /// <summary>
    /// Trims an optional URL or throws, for use inside domain factory methods.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <param name="parameterName">Name reported by the exception.</param>
    /// <returns>The trimmed URL, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">The value is not a safe absolute web address.</exception>
    public static string? NormalizeOrThrow(string? value, string parameterName)
    {
        if (!TryNormalize(value, out var normalized))
        {
            throw new ArgumentException(ValidationMessage, parameterName);
        }

        return normalized;
    }

    /// <summary>
    /// Reports whether a value would be accepted.
    /// </summary>
    /// <param name="value">Candidate URL.</param>
    [SuppressMessage(
        "Design",
        "CA1054:URI-like parameters should not be strings",
        Justification = "The value being validated is untrusted client input, not a Uri.")]
    public static bool IsAcceptable(string? value) => TryNormalize(value, out _);
}
