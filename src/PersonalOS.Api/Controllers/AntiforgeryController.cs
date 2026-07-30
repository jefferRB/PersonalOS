using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalOS.Api.Contracts.Auth;

namespace PersonalOS.Api.Controllers;

[ApiController]
[Route("api/antiforgery")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AntiforgeryController(
    IAntiforgery antiforgery,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string RequestTokenCookieName = "XSRF-TOKEN";

    [HttpGet("token")]
    [AllowAnonymous]
    [ProducesResponseType<AntiforgeryTokenResponse>(StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenResponse> GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var requestToken = tokens.RequestToken ?? string.Empty;

        Response.Cookies.Append(
            RequestTokenCookieName,
            requestToken,
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !environment.IsDevelopment(),
            });

        return Ok(new AntiforgeryTokenResponse(requestToken));
    }
}
