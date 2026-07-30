using System.Threading.RateLimiting;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PersonalOS.Api.Filters;
using PersonalOS.Api.Health;
using PersonalOS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var secureCookiePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.None
    : CookieSecurePolicy.Always;

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path,
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" },
        };
    };
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ValidateAntiforgeryTokenForUnsafeMethodsFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ValidateAntiforgeryTokenForUnsafeMethodsFilter>();
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "PersonalOS.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookiePolicy;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PersonalOS.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookiePolicy;
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context => WriteAuthenticationProblemAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized.",
            "Authentication is required."),
        OnRedirectToAccessDenied = context => WriteAuthenticationProblemAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden.",
            "You do not have permission to access this resource."),
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Too many attempts. Try again later.",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";
        SetNoStoreHeaders(httpContext.Response);
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            cancellationToken: cancellationToken);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Frame-Options"] = "DENY";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        return Task.CompletedTask;
    });

    await next();
});

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync,
});

app.Run();

static string GetClientPartitionKey(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static Task WriteAuthenticationProblemAsync(
    HttpContext httpContext,
    int statusCode,
    string title,
    string detail)
{
    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = httpContext.Request.Path,
    };
    problem.Extensions["traceId"] = httpContext.TraceIdentifier;

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/problem+json";
    SetNoStoreHeaders(httpContext.Response);

    return JsonSerializer.SerializeAsync(httpContext.Response.Body, problem);
}

static Task WriteHealthResponseAsync(HttpContext httpContext, HealthReport report)
{
    httpContext.Response.ContentType = "application/json";

    return httpContext.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
    });
}

static void SetNoStoreHeaders(HttpResponse response)
{
    response.Headers["Cache-Control"] = "no-store";
    response.Headers["Pragma"] = "no-cache";
    response.Headers["Expires"] = "0";
}

public partial class Program;
