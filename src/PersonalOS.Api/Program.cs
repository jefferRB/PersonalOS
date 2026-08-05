using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
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
builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddService<ValidateAntiforgeryTokenForUnsafeMethodsFilter>();
    })
    .AddJsonOptions(options =>
    {
        // The daily modules use enumerations such as meal type and recurrence frequency. Sending
        // them as names keeps the contract readable and stable: inserting a new value never shifts
        // the meaning of an existing one, which a numeric contract would allow.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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

    // Profile updates are user-initiated and infrequent. The limit stops an abusive write loop
    // without interfering with normal editing.
    options.AddPolicy("profile", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    // The daily modules are limited on writes only. Reading Today, the calendar, or a routine is
    // ordinary navigation and stays unlimited: a limit there would make the application feel
    // broken long before it stopped anything abusive.
    //
    // Calendar writes are the busiest: checking off a morning of occurrences is a burst of requests
    // from one honest user, so the limit is the highest of the group.
    AddWritePolicy(options, "calendar", permitLimit: 120);

    // A routine session is saved after each step, so recording a workout is also bursty.
    AddWritePolicy(options, "routines", permitLimit: 120);

    // Meals are entered a few times a day, with edits.
    AddWritePolicy(options, "nutrition", permitLimit: 90);

    // Study sessions are recorded once per block.
    AddWritePolicy(options, "study", permitLimit: 90);

    // The journal is written once per day and edited while writing. The limit is the strictest of
    // the group because this endpoint carries the most sensitive text and needs the least
    // throughput, but it still allows a save every two seconds for a full minute.
    AddWritePolicy(options, "journal", permitLimit: 30);
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

/// <summary>
/// Registers a fixed one-minute window policy for the write endpoints of a daily module.
/// </summary>
static void AddWritePolicy(RateLimiterOptions options, string policyName, int permitLimit) =>
    options.AddPolicy(policyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

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
