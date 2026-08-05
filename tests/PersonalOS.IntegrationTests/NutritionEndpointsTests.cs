using System.Net;

namespace PersonalOS.IntegrationTests;

/// <summary>
/// The nutrition and meal endpoints.
/// </summary>
public sealed class NutritionEndpointsTests
{
    private const string LocalDate = "2026-07-30";

    [Fact]
    public async Task EveryNutritionRouteRejectsAnAnonymousCaller()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = DailyApi.CreateClient(factory);
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync($"/api/nutrition/day?date={LocalDate}"),
            await client.GetAsync("/api/nutrition/goal"),
            await DailyApi.SendAsync(client, HttpMethod.Put, "/api/nutrition/goal", Goal(2000)),
            await DailyApi.SendAsync(client, HttpMethod.Post, "/api/meals", Meal("Oats", 420)),
            await DailyApi.SendAsync(client, HttpMethod.Put, $"/api/meals/{id}", Meal("Oats", 420)),
            await DailyApi.SendAsync(client, HttpMethod.Delete, $"/api/meals/{id}"),
        };

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task AnAccountWithNoGoalReportsNoTarget()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var day = await DailyApi.GetAsync<NutritionDayDto>(
            client,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Null(day!.Goal.DailyCalorieTarget);
        Assert.Null(day.RemainingCalories);
        Assert.Equal(0, day.ConsumedCalories);
    }

    [Fact]
    public async Task RecordingMealsAccumulatesTheDayTotalAgainstTheTarget()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/nutrition/goal", Goal(2000));

        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/meals", Meal("Oats", 420));
        await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/meals",
            Meal("Rice and chicken", 700) with { mealType = "lunch" });

        var day = await DailyApi.GetAsync<NutritionDayDto>(
            client,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Equal(1120, day!.ConsumedCalories);
        Assert.Equal(880, day.RemainingCalories);
        Assert.Equal(2, day.Meals.Count);
    }

    [Fact]
    public async Task GoingOverTheTargetReportsANegativeRemainderRatherThanAnError()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        await DailyApi.SendAsync(client, HttpMethod.Put, "/api/nutrition/goal", Goal(2000));
        await DailyApi.SendAsync(client, HttpMethod.Post, "/api/meals", Meal("Large dinner", 2400));

        var response = await client.GetAsync($"/api/nutrition/day?date={LocalDate}");
        var day = await DailyApi.ReadAsync<NutritionDayDto>(response);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(-400, day!.RemainingCalories);

        // The API returns arithmetic. It must never return advice or a judgement.
        foreach (var forbidden in new[] { "unhealthy", "too many", "warning", "recommend" })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task OptionalMacrosAreStoredAndSummed()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/meals",
            Meal("Oats", 420) with { proteinGrams = 12.5m, carbohydrateGrams = 70m, fatGrams = 8m });

        var day = await DailyApi.GetAsync<NutritionDayDto>(
            client,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Equal(12.5m, day!.ProteinGrams);
        Assert.Equal(70m, day.CarbohydrateGrams);
        Assert.Equal(8m, day.FatGrams);
    }

    [Fact]
    public async Task AMealCanBeEditedAndDeleted()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);
        var created = await CreateMealAsync(client);

        var update = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            $"/api/meals/{created.Id}",
            Meal("Oats and banana", 500));
        var updated = await DailyApi.ReadAsync<MealDto>(update);

        var delete = await DailyApi.SendAsync(client, HttpMethod.Delete, $"/api/meals/{created.Id}");
        var day = await DailyApi.GetAsync<NutritionDayDto>(
            client,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Equal(500, updated!.Calories);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(day!.Meals);
        Assert.Equal(0, day.ConsumedCalories);
    }

    [Theory]
    [InlineData(499)]
    [InlineData(20001)]
    public async Task ACalorieTargetOutsideTheStoredRangeIsRejected(int target)
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Put,
            "/api/nutrition/goal",
            Goal(target));
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("dailyCalorieTarget"));
    }

    [Fact]
    public async Task ANegativeCalorieValueIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/meals",
            Meal("Oats", -1));
        var problem = await DailyApi.ReadAsync<ValidationProblemDto>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey("calories"));
    }

    [Fact]
    public async Task AMealWriteWithoutAnAntiforgeryTokenIsRejected()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var response = await DailyApi.SendWithoutAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/meals",
            Meal("Oats", 420));
        var day = await DailyApi.GetAsync<NutritionDayDto>(
            client,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(day!.Meals);
    }

    [Fact]
    public async Task OverPostedOwnershipFieldsAreIgnoredOnAMeal()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(clientA, HttpMethod.Post, "/api/meals", new
        {
            localDate = LocalDate,
            mealType = "breakfast",
            name = "Oats",
            calories = 420,
            userId = Guid.NewGuid(),
            id = Guid.NewGuid(),
        });

        var dayB = await DailyApi.GetAsync<NutritionDayDto>(
            clientB,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Empty(dayB!.Meals);
    }

    [Fact]
    public async Task TwoAccountsKeepIndependentGoalsAndMeals()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");

        await DailyApi.SendAsync(clientA, HttpMethod.Put, "/api/nutrition/goal", Goal(2000));
        await DailyApi.SendAsync(clientA, HttpMethod.Post, "/api/meals", Meal("Oats", 420));

        var dayB = await DailyApi.GetAsync<NutritionDayDto>(
            clientB,
            $"/api/nutrition/day?date={LocalDate}");

        Assert.Null(dayB!.Goal.DailyCalorieTarget);
        Assert.Equal(0, dayB.ConsumedCalories);
        Assert.Empty(dayB.Meals);
    }

    [Fact]
    public async Task AnotherAccountsMealReturnsNotFound()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var clientA = await DailyApi.SignInAsync(factory, "Account A");
        using var clientB = await DailyApi.SignInAsync(factory, "Account B");
        var meal = await CreateMealAsync(clientA);

        var update = await DailyApi.SendAsync(
            clientB,
            HttpMethod.Put,
            $"/api/meals/{meal.Id}",
            Meal("Hijacked", 1));
        var delete = await DailyApi.SendAsync(clientB, HttpMethod.Delete, $"/api/meals/{meal.Id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task NutritionResponsesUseNoStoreCaching()
    {
        await using var factory = new PersonalOSWebApplicationFactory();
        using var client = await DailyApi.SignInAsync(factory);

        var day = await client.GetAsync($"/api/nutrition/day?date={LocalDate}");
        var goal = await client.GetAsync("/api/nutrition/goal");

        Assert.True(day.Headers.CacheControl?.NoStore);
        Assert.True(goal.Headers.CacheControl?.NoStore);
    }

    private static async Task<MealDto> CreateMealAsync(HttpClient client)
    {
        var response = await DailyApi.SendAsync(
            client,
            HttpMethod.Post,
            "/api/meals",
            Meal("Oats", 420));
        response.EnsureSuccessStatusCode();

        return (await DailyApi.ReadAsync<MealDto>(response))!;
    }

    private static GoalRequest Goal(int target) => new(target, null, null, null);

    private static MealRequest Meal(string name, int calories) =>
        new(LocalDate, "breakfast", name, null, calories, null, null, null, null, null);

    private sealed record GoalRequest(
        int? dailyCalorieTarget,
        decimal? proteinTargetGrams,
        decimal? carbohydrateTargetGrams,
        decimal? fatTargetGrams);

    private sealed record MealRequest(
        string localDate,
        string mealType,
        string name,
        string? quantity,
        int? calories,
        decimal? proteinGrams,
        decimal? carbohydrateGrams,
        decimal? fatGrams,
        string? occurredAtLocalTime,
        string? notes);
}
