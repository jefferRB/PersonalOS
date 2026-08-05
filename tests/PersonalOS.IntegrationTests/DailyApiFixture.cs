using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// Shared helpers for the daily module endpoint tests.
/// </summary>
/// <remarks>
/// Every helper goes through the real HTTP pipeline, so authentication, antiforgery, model
/// binding, validation, and serialization are all exercised exactly as a browser would exercise
/// them.
/// </remarks>
public static class DailyApi
{
    /// <summary>Password used by the test accounts.</summary>
    public const string StrongPassword = "Password123";

    /// <summary>
    /// JSON options matching the API, so responses deserialize the way a client would read them.
    /// </summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Creates a client that keeps cookies, like a browser.
    /// </summary>
    /// <param name="factory">Test host.</param>
    public static HttpClient CreateClient(PersonalOSWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    /// <summary>
    /// Registers and signs in a new account, returning its client.
    /// </summary>
    /// <param name="factory">Test host.</param>
    /// <param name="displayName">Display name for the account.</param>
    public static async Task<HttpClient> SignInAsync(
        PersonalOSWebApplicationFactory factory,
        string displayName = "Jefferson")
    {
        var client = CreateClient(factory);
        var email = NewEmail();

        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new { displayName, email, password = StrongPassword });

        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = StrongPassword, rememberMe = false });

        return client;
    }

    /// <summary>
    /// Sends a state-changing request with a fresh antiforgery token.
    /// </summary>
    /// <param name="client">Signed-in client.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="url">Request path.</param>
    /// <param name="body">Optional request body.</param>
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        await AddAntiforgeryHeaderAsync(client);

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a state-changing request without any antiforgery token.
    /// </summary>
    /// <param name="client">Signed-in client.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="url">Request path.</param>
    /// <param name="body">Optional request body.</param>
    public static async Task<HttpResponseMessage> SendWithoutAntiforgeryAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a state-changing request carrying a token that was never issued.
    /// </summary>
    /// <param name="client">Signed-in client.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="url">Request path.</param>
    /// <param name="body">Optional request body.</param>
    public static async Task<HttpResponseMessage> SendWithInvalidAntiforgeryAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid-request-token");

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Reads a JSON response using the API's serializer settings.
    /// </summary>
    /// <typeparam name="TValue">Expected shape.</typeparam>
    /// <param name="response">Response to read.</param>
    public static async Task<TValue?> ReadAsync<TValue>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<TValue>(Json);

    /// <summary>
    /// Performs a GET and deserializes the body.
    /// </summary>
    /// <typeparam name="TValue">Expected shape.</typeparam>
    /// <param name="client">Signed-in client.</param>
    /// <param name="url">Request path.</param>
    public static async Task<TValue?> GetAsync<TValue>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await ReadAsync<TValue>(response);
    }

    /// <summary>Fetches a fresh antiforgery token and attaches it to the client.</summary>
    public static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/antiforgery/token");

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private sealed record AntiforgeryTokenResponse(string RequestToken);
}

/// <summary>Recurrence rule, as the calendar endpoints return it.</summary>
public sealed record CalendarRecurrenceDto(
    string Frequency,
    int Interval,
    DateOnly? EndDate,
    IReadOnlyList<string> SelectedWeekdays);

/// <summary>Calendar item with its rule, as the API returns it.</summary>
public sealed record PlanningItemDto(
    Guid Id,
    string Title,
    string? Description,
    string Kind,
    string Category,
    string Priority,
    DateOnly StartDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    CalendarRecurrenceDto Recurrence,
    bool IsRecurrencePatternLocked);

/// <summary>One calendar item on one local day, as the API returns it.</summary>
public sealed record CalendarOccurrenceDto(
    Guid PlanningItemId,
    DateOnly OccurrenceDate,
    string Title,
    string? Description,
    string Kind,
    string Category,
    string Priority,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string Status,
    bool IsRecurring,
    bool IsImportant,
    DateTimeOffset? CompletedAtUtc);

/// <summary>One month cell summary, as the API returns it.</summary>
public sealed record CalendarDaySummaryDto(
    DateOnly Date,
    int TotalCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount,
    IReadOnlyList<DayKindCountDto> Kinds,
    bool HasHighPriority);

/// <summary>How many of one kind fall on a day, as the API returns it.</summary>
public sealed record DayKindCountDto(string Kind, int Count);

/// <summary>One month of the grid, as the API returns it.</summary>
public sealed record CalendarMonthDto(
    int Year,
    int Month,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<CalendarDaySummaryDto> Days);

/// <summary>One local day, as the API returns it.</summary>
public sealed record CalendarDayDto(
    DateOnly Date,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceDto> Occurrences);

/// <summary>One day inside the upcoming window, as the API returns it.</summary>
public sealed record UpcomingDayDto(
    DateOnly Date,
    IReadOnlyList<CalendarOccurrenceDto> Occurrences);

/// <summary>The next seven days, as the API returns them.</summary>
public sealed record UpcomingWeekDto(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly TodayLocalDate,
    string TimeZoneId,
    IReadOnlyList<UpcomingDayDto> Days);

/// <summary>Routine, as the API returns it.</summary>
public sealed record RoutineDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    RecurrenceDto Recurrence,
    bool IsActive,
    IReadOnlyList<RoutineStepDto> Steps);

/// <summary>Recurrence rule, as the API returns it.</summary>
public sealed record RecurrenceDto(
    string Frequency,
    int Interval,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<string> SelectedWeekdays);

/// <summary>Routine step, as the API returns it.</summary>
public sealed record RoutineStepDto(
    Guid Id,
    int Order,
    string Title,
    string StepType,
    int? TargetSets,
    int? TargetRepetitions,
    decimal? TargetWeight,
    int? TargetDurationMinutes,
    string? Notes);

/// <summary>Routine session, as the API returns it.</summary>
public sealed record RoutineSessionDto(
    Guid Id,
    Guid RoutineTemplateId,
    string RoutineName,
    string Category,
    DateOnly LocalDate,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Notes,
    IReadOnlyList<RoutineStepDto> Steps,
    IReadOnlyList<RoutineStepResultDto> StepResults);

/// <summary>Step result, as the API returns it.</summary>
public sealed record RoutineStepResultDto(
    Guid RoutineStepId,
    bool IsCompleted,
    int? ActualSets,
    int? ActualRepetitions,
    decimal? ActualWeight,
    int? ActualDurationMinutes,
    string? Notes);

/// <summary>Routine occurrence, as the API returns it.</summary>
public sealed record RoutineOccurrenceDto(
    Guid RoutineTemplateId,
    string Name,
    string Category,
    DateOnly LocalDate,
    int StepCount,
    Guid? SessionId,
    bool IsCompleted,
    int CompletedStepCount);

/// <summary>Meal, as the API returns it.</summary>
public sealed record MealDto(
    Guid Id,
    DateOnly LocalDate,
    string MealType,
    string Name,
    string? Quantity,
    int Calories,
    decimal? ProteinGrams,
    decimal? CarbohydrateGrams,
    decimal? FatGrams,
    TimeOnly? OccurredAtLocalTime,
    string? Notes);

/// <summary>Nutrition goal, as the API returns it.</summary>
public sealed record NutritionGoalDto(
    int? DailyCalorieTarget,
    decimal? ProteinTargetGrams,
    decimal? CarbohydrateTargetGrams,
    decimal? FatTargetGrams,
    DateTimeOffset? UpdatedAtUtc);

/// <summary>Nutrition day, as the API returns it.</summary>
public sealed record NutritionDayDto(
    DateOnly LocalDate,
    NutritionGoalDto Goal,
    int ConsumedCalories,
    int? RemainingCalories,
    decimal ProteinGrams,
    decimal CarbohydrateGrams,
    decimal FatGrams,
    IReadOnlyList<MealDto> Meals);

/// <summary>Study project, as the API returns it.</summary>
public sealed record StudyProjectDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<StudyResourceDto> Resources);

/// <summary>Study resource, as the API returns it.</summary>
public sealed record StudyResourceDto(
    Guid Id,
    string Title,
    string ResourceType,
    string? ExternalUrl,
    string? Notes);

/// <summary>Study session, as the API returns it.</summary>
public sealed record StudySessionDto(
    Guid Id,
    Guid StudyProjectId,
    string ProjectName,
    DateOnly LocalDate,
    TimeOnly? StartTime,
    int DurationMinutes,
    string? Summary,
    string? ProgressNote);

/// <summary>Journal entry, as the API returns it.</summary>
public sealed record JournalEntryDto(
    DateOnly LocalDate,
    string? WentWell,
    string? WentPoorly,
    string? Cause,
    string? Lesson,
    string? AdjustmentForTomorrow,
    string? FreeNotes,
    DateTimeOffset? UpdatedAtUtc,
    bool HasContent);

/// <summary>Today summary, as the API returns it.</summary>
public sealed record TodaySummaryDto(
    DateOnly LocalDate,
    string TimeZoneId,
    bool IsToday,
    TimeOnly LocalTimeOfDay,
    IReadOnlyList<CalendarOccurrenceDto> Occurrences,
    IReadOnlyList<RoutineOccurrenceDto> Routines,
    NutritionDayDto Nutrition,
    IReadOnlyList<StudySessionDto> StudySessions,
    TodayProgressDto Progress);

/// <summary>Today progress counters, as the API returns them.</summary>
public sealed record TodayProgressDto(
    int PlannedItemCount,
    int CompletedItemCount,
    int RoutineCount,
    int CompletedRoutineCount,
    int StudyMinutes,
    int ConsumedCalories,
    int? DailyCalorieTarget,
    bool JournalCompleted);

/// <summary>How the planner's timeline is shown, as the API returns it.</summary>
public sealed record CalendarDisplayDto(
    TimeOnly DayStartTime,
    TimeOnly DayEndTime,
    int SlotMinutes);

/// <summary>Profile of the authenticated account, as the API returns it.</summary>
public sealed record UserProfileDto(
    string DisplayName,
    string Email,
    string TimeZoneId,
    CalendarDisplayDto CalendarDisplay,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Validation Problem Details, as the API returns them.</summary>
public sealed record ValidationProblemDto(
    string? Title,
    int? Status,
    Dictionary<string, string[]>? Errors);
