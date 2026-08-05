using PersonalOS.Application.Common;
using PersonalOS.Application.Routines;
using PersonalOS.Domain.Routines;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Daily;

/// <summary>
/// Routine editing and workout recording.
/// </summary>
public sealed class RoutineServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");
    private static readonly DateOnly Monday = new(2026, 7, 27);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 27, 13, 0, 0, TimeSpan.Zero);

    private readonly InMemoryRoutineStore store = new();
    private readonly RoutineService service;

    public RoutineServiceTests()
    {
        service = new RoutineService(store, new FixedClock(UtcNow));
    }

    [Fact]
    public async Task CreatingARoutineStoresItsStepsInTheOrderTheyWereSent()
    {
        var result = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["Bench press", "Incline dumbbell press", "Pec deck"],
            result.Value!.Steps.Select(step => step.Title));
        Assert.Equal([0, 1, 2], result.Value.Steps.Select(step => step.Order));
    }

    [Fact]
    public async Task StepPositionsAreRenumberedSoTwoStepsCannotClaimTheSamePlace()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);

        // The client sends the list in the order the user arranged it; the server owns the numbers.
        var reordered = ChestWorkout() with
        {
            Steps =
            [
                ExerciseStep("Pec deck"),
                ExerciseStep("Bench press"),
                ExerciseStep("Incline dumbbell press"),
            ],
        };

        var updated = await service.UpdateAsync(
            UserA,
            created.Value!.Id,
            reordered,
            CancellationToken.None);

        Assert.Equal("Pec deck", updated.Value!.Steps[0].Title);
        Assert.Equal([0, 1, 2], updated.Value.Steps.Select(step => step.Order));
    }

    [Fact]
    public async Task ExerciseTargetsAreKeptOnlyForExerciseSteps()
    {
        var input = ChestWorkout() with
        {
            Steps =
            [
                new RoutineStepInput("Stretch", RoutineStepType.Checklist, 3, 10, 60m, 20, null),
            ],
        };

        var result = await service.CreateAsync(UserA, input, CancellationToken.None);
        var step = result.Value!.Steps[0];

        // A checklist step that kept a stale weight would show numbers the user never intended.
        Assert.Null(step.TargetSets);
        Assert.Null(step.TargetWeight);
        Assert.Null(step.TargetDurationMinutes);
    }

    [Fact]
    public async Task ARoutineWithoutANameIsRejected()
    {
        var result = await service.CreateAsync(
            UserA,
            ChestWorkout() with { Name = "   " },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(RoutineService.NameField));
    }

    [Fact]
    public async Task AStepWithoutATitleIsRejected()
    {
        var result = await service.CreateAsync(
            UserA,
            ChestWorkout() with { Steps = [ExerciseStep("  ")] },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(RoutineService.StepsField));
    }

    [Fact]
    public async Task AnInvalidRecurrenceIntervalIsRejected()
    {
        var result = await service.CreateAsync(
            UserA,
            ChestWorkout() with { Recurrence = Weekly() with { Interval = 0 } },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(RoutineService.IntervalField));
    }

    [Fact]
    public async Task SelectedWeekdaysWithNoWeekdayIsRejected()
    {
        var result = await service.CreateAsync(
            UserA,
            ChestWorkout() with
            {
                Recurrence = Weekly() with
                {
                    Frequency = RecurrenceFrequency.SelectedWeekdays,
                    SelectedWeekdays = [],
                },
            },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(RoutineService.SelectedWeekdaysField));
    }

    [Fact]
    public async Task OccurrencesAppearOnEveryMatchingDayWithoutStoringARow()
    {
        await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);

        var occurrences = await service.GetOccurrencesAsync(
            UserA,
            Monday,
            Monday.AddDays(27),
            CancellationToken.None);

        Assert.Equal(4, occurrences.Count);
        Assert.All(occurrences, occurrence => Assert.Null(occurrence.SessionId));
        Assert.All(occurrences, occurrence => Assert.False(occurrence.IsCompleted));
    }

    [Fact]
    public async Task AnInactiveRoutineDisappearsFromOccurrencesButKeepsItsHistory()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);
        await service.StartSessionAsync(UserA, created.Value!.Id, Monday, CancellationToken.None);

        await service.UpdateAsync(
            UserA,
            created.Value.Id,
            ChestWorkout() with { IsActive = false },
            CancellationToken.None);

        var occurrences = await service.GetOccurrencesAsync(
            UserA,
            Monday,
            Monday.AddDays(27),
            CancellationToken.None);
        var routine = await service.GetTemplateAsync(UserA, created.Value.Id, CancellationToken.None);

        Assert.Empty(occurrences);
        Assert.True(routine.IsSuccess);
        Assert.False(routine.Value!.IsActive);
    }

    [Fact]
    public async Task StartingASessionCreatesOneEmptyResultPerStep()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);

        var session = await service.StartSessionAsync(
            UserA,
            created.Value!.Id,
            Monday,
            CancellationToken.None);

        Assert.True(session.IsSuccess);
        Assert.Equal(3, session.Value!.StepResults.Count);
        Assert.All(session.Value.StepResults, result => Assert.False(result.IsCompleted));
        Assert.Null(session.Value.CompletedAtUtc);
    }

    [Fact]
    public async Task StartingTheSameDayTwiceReturnsTheSameSession()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);

        var first = await service.StartSessionAsync(
            UserA,
            created.Value!.Id,
            Monday,
            CancellationToken.None);
        var second = await service.StartSessionAsync(
            UserA,
            created.Value.Id,
            Monday,
            CancellationToken.None);

        // Pressing the button twice must not create two histories for the same morning.
        Assert.Equal(first.Value!.Id, second.Value!.Id);
    }

    [Fact]
    public async Task PartialProgressIsSavedWithoutCompletingTheSession()
    {
        var session = await StartChestSessionAsync();
        var firstStepId = session.Steps[0].Id;

        var saved = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(
                null,
                IsCompleted: false,
                [new RoutineStepResultInput(firstStepId, true, 4, 8, 62.5m, null, "Felt strong")]),
            CancellationToken.None);

        var result = saved.Value!.StepResults.Single(item => item.RoutineStepId == firstStepId);

        Assert.Null(saved.Value.CompletedAtUtc);
        Assert.True(result.IsCompleted);
        Assert.Equal(4, result.ActualSets);
        Assert.Equal(8, result.ActualRepetitions);
        Assert.Equal(62.5m, result.ActualWeight);
    }

    [Fact]
    public async Task CompletingTheSessionRecordsTheInstantOnce()
    {
        var session = await StartChestSessionAsync();

        var first = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(null, IsCompleted: true, []),
            CancellationToken.None);
        var second = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(null, IsCompleted: true, []),
            CancellationToken.None);

        Assert.NotNull(first.Value!.CompletedAtUtc);
        Assert.Equal(first.Value.CompletedAtUtc, second.Value!.CompletedAtUtc);
    }

    [Fact]
    public async Task ACompletedSessionCanBeReopened()
    {
        var session = await StartChestSessionAsync();
        await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(null, IsCompleted: true, []),
            CancellationToken.None);

        var reopened = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(null, IsCompleted: false, []),
            CancellationToken.None);

        Assert.Null(reopened.Value!.CompletedAtUtc);
    }

    [Fact]
    public async Task AStepFromAnotherRoutineIsRejected()
    {
        var session = await StartChestSessionAsync();

        var result = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(
                null,
                IsCompleted: false,
                [new RoutineStepResultInput(Guid.NewGuid(), true, null, null, null, null, null)]),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(RoutineService.StepResultsField));
    }

    [Fact]
    public async Task AnOutOfRangeWorkoutValueIsRejected()
    {
        var session = await StartChestSessionAsync();

        var result = await service.SaveSessionAsync(
            UserA,
            session.Id,
            new RoutineSessionInput(
                null,
                IsCompleted: false,
                [new RoutineStepResultInput(session.Steps[0].Id, true, -1, null, null, null, null)]),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task OneAccountCannotSeeOrChangeAnotherAccountsRoutine()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);
        var id = created.Value!.Id;

        var read = await service.GetTemplateAsync(UserB, id, CancellationToken.None);
        var update = await service.UpdateAsync(UserB, id, ChestWorkout(), CancellationToken.None);
        var start = await service.StartSessionAsync(UserB, id, Monday, CancellationToken.None);
        var deleted = await service.DeleteAsync(UserB, id, CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, read.Status);
        Assert.Equal(OperationStatus.NotFound, update.Status);
        Assert.Equal(OperationStatus.NotFound, start.Status);
        Assert.False(deleted);
        Assert.Empty(await service.GetTemplatesAsync(UserB, activeOnly: false, CancellationToken.None));
    }

    [Fact]
    public async Task OneAccountCannotSaveProgressOnAnotherAccountsSession()
    {
        var session = await StartChestSessionAsync();

        var result = await service.SaveSessionAsync(
            UserB,
            session.Id,
            new RoutineSessionInput(null, IsCompleted: true, []),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    private async Task<RoutineSessionRecord> StartChestSessionAsync()
    {
        var created = await service.CreateAsync(UserA, ChestWorkout(), CancellationToken.None);
        var session = await service.StartSessionAsync(
            UserA,
            created.Value!.Id,
            Monday,
            CancellationToken.None);

        return session.Value!;
    }

    private static RoutineTemplateInput ChestWorkout() =>
        new(
            "Monday - Chest",
            null,
            RoutineCategory.Workout,
            Weekly(),
            IsActive: true,
            [
                ExerciseStep("Bench press"),
                ExerciseStep("Incline dumbbell press"),
                ExerciseStep("Pec deck"),
            ]);

    private static RecurrenceInput Weekly() =>
        new(RecurrenceFrequency.Weekly, 1, Monday, null, []);

    private static RoutineStepInput ExerciseStep(string title) =>
        new(title, RoutineStepType.Exercise, 3, 10, 60m, null, null);
}
