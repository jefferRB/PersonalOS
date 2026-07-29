using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalOS.Api.Contracts.Auth;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    TimeProvider timeProvider,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthMessageResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            logger.LogInformation("Registration failed because email is already registered.");

            return ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["An account with this email already exists."],
            });
        }

        var user = new AppUser
        {
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            UserName = email,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToArray();
            logger.LogInformation(
                "Registration failed with {ErrorCount} identity validation errors.",
                errors.Length);

            return ValidationProblem(MapIdentityErrors(errors));
        }

        logger.LogInformation("Registration succeeded for user {UserId}.", user.Id);

        return Ok(new AuthMessageResponse("AccountCreated"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim();
        var result = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Signed-in user could not be loaded.");

            logger.LogInformation("Login succeeded for user {UserId}.", user.Id);

            return Ok(CurrentUserResponse.FromUser(user));
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Login blocked because the account is locked out.");

            return Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account temporarily locked.",
                detail: "Too many failed login attempts. Try again later.");
        }

        logger.LogInformation("Login failed.");

        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid credentials.",
            detail: "The email or password is incorrect.");
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.GetUserAsync(User);

        return user is null
            ? Unauthorized()
            : Ok(CurrentUserResponse.FromUser(user));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = userManager.GetUserId(User);

        await signInManager.SignOutAsync();

        logger.LogInformation("Logout succeeded for user {UserId}.", userId);

        return NoContent();
    }

    private ActionResult ValidationProblem(Dictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return BadRequest(problem);
    }

    private static Dictionary<string, string[]> MapIdentityErrors(IdentityError[] errors)
    {
        var groupedErrors = errors
            .GroupBy(error => GetFieldName(error.Code))
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return groupedErrors.Count > 0
            ? groupedErrors
            : new Dictionary<string, string[]> { [""] = ["The account could not be created."] };
    }

    private static string GetFieldName(string code)
    {
        if (code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
        {
            return "password";
        }

        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase)
            || code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return "email";
        }

        return "";
    }
}
