using System.Security.Claims;

namespace PersonalOS.Api.Security;

/// <summary>
/// Helpers for reading the authenticated account identifier from the request principal.
/// </summary>
/// <remarks>
/// The identifier always comes from the authentication cookie that ASP.NET Core Identity issued.
/// It is never read from a route value, a query string, or a request body.
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Attempts to read the authenticated account identifier.
    /// </summary>
    /// <param name="principal">Request principal.</param>
    /// <param name="userId">Parsed account identifier when the principal carries one.</param>
    /// <returns><see langword="true"/> when the principal carries a usable identifier.</returns>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return value is not null
            && Guid.TryParse(value, out userId)
            && userId != Guid.Empty;
    }
}
