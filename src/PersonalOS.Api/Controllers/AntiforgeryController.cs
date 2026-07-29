using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalOS.Api.Contracts.Auth;

namespace PersonalOS.Api.Controllers;

[ApiController]
[Route("api/antiforgery")]
public sealed class AntiforgeryController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    [AllowAnonymous]
    [ProducesResponseType<AntiforgeryTokenResponse>(StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenResponse> GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new AntiforgeryTokenResponse(tokens.RequestToken ?? string.Empty));
    }
}
