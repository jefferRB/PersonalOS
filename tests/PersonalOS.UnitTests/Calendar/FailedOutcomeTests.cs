using PersonalOS.Application.Calendar;
using PersonalOS.Application.Common;
using PersonalOS.Application.Time;
using PersonalOS.Domain.Planning;
using PersonalOS.UnitTests.Daily;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Calendar;

/// <summary>
/// The "failed" outcome: recording that something expected did not happen.
/// </summary>
/// <remarks>
/// Failed is deliberately distinct from cancelled. Calling something off in advance and not doing
/// something you meant to do are different facts about a day, and the tests below pin down both the
/// distinction and the rule that only a day which has already arrived can be failed.
/// </remarks>
public sealed class FailedOutcomeTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");

    // 19:24 UTC on 30 July is still 13:24 on 30 July in Costa Rica.
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);
    private static readonly DateOnly LocalDate = new(2026, 7, 30);

    private readonly InMemoryCalendarStore store = new();
    private readonly CalendarService service;

    public FailedOutcomeTests()
    {
        service = Build(store, "America/Costa_Rica", UtcNow);
    }

    [Fact]
    public void TheStoredNumbersOfTheExistingOutcomesNeverMove()
    {
        // These values are in the database. Reordering them would silently reinterpret every row
        // ever written, so they are asserted rather than assumed.
        Assert.Equal(0, (int)OccurrenceStatus.Planned);
        Assert.Equal(1, (int)OccurrenceStatus.Completed);
        Assert.Equal(2, (int)OccurrenceStatus.Cancelled);
    }

    [Fact]
    public void FailedIsAppendedAfterTheExistingOutcomes()
    {
        Assert.Equal(3, (int)OccurrenceStatus.Failed);
    }

    [Fact]
    public async Task TodayCanBeMarkedFailed()
    {
        var created = await CreateAsync(LocalDate);

        var result = await SetStatusAsync(created, LocalDate, OccurrenceStatus.Failed);

        Assert.True(result.IsSuccess);
        Assert.Equal(OccurrenceStatus.Failed, result.Value!.Status);
        Assert.Null(result.Value.CompletedAtUtc);
    }

    [Fact]
    public async Task APastOccurrenceCanBeMarkedFailed()
    {
        var created = await CreateAsync(LocalDate.AddDays(-3));

        var result = await SetStatusAsync(created, LocalDate.AddDays(-3), OccurrenceStatus.Failed);

        Assert.True(result.IsSuccess);
        Assert.Equal(OccurrenceStatus.Failed, result.Value!.Status);
    }

    [Fact]
    public async Task AFutureOccurrenceCannotBeMarkedFailed()
    {
        var created = await CreateAsync(LocalDate.AddDays(1));

        var result = await SetStatusAsync(created, LocalDate.AddDays(1), OccurrenceStatus.Failed);

        // A day that has not arrived has not had its chance yet, so calling it failed would be a
        // claim about something that has not happened.
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.OccurrenceDateField));
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task TheFutureBoundaryFollowsTheAccountsOwnTimeZone()
    {
        // 03:00 UTC on 31 July is still 30 July in Costa Rica but already the 31st in Tokyo, so the
        // same date is future for one account and current for the other.
        var instant = new DateTimeOffset(2026, 7, 31, 3, 0, 0, TimeSpan.Zero);
        var target = new DateOnly(2026, 7, 31);

        var costaRicaStore = new InMemoryCalendarStore();
        var costaRica = Build(costaRicaStore, "America/Costa_Rica", instant);
        var tokyoStore = new InMemoryCalendarStore();
        var tokyo = Build(tokyoStore, "Asia/Tokyo", instant);

        var costaRicaItem = await CreateAsync(costaRica, target);
        var tokyoItem = await CreateAsync(tokyo, target);

        var costaRicaResult = await costaRica.SetOccurrenceStatusAsync(
            UserA,
            costaRicaItem,
            target,
            OccurrenceStatus.Failed,
            CancellationToken.None);
        var tokyoResult = await tokyo.SetOccurrenceStatusAsync(
            UserA,
            tokyoItem,
            target,
            OccurrenceStatus.Failed,
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, costaRicaResult.Status);
        Assert.True(tokyoResult.IsSuccess);
    }

    [Fact]
    public async Task MarkingFailedTwiceIsIdempotent()
    {
        var created = await CreateAsync(LocalDate);

        await SetStatusAsync(created, LocalDate, OccurrenceStatus.Failed);
        var second = await SetStatusAsync(created, LocalDate, OccurrenceStatus.Failed);

        Assert.True(second.IsSuccess);
        Assert.Equal(OccurrenceStatus.Failed, second.Value!.Status);
        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task ReopeningAFailedOccurrenceReturnsItToPlanned()
    {
        var created = await CreateAsync(LocalDate);
        await SetStatusAsync(created, LocalDate, OccurrenceStatus.Failed);

        var result = await SetStatusAsync(created, LocalDate, OccurrenceStatus.Planned);

        Assert.Equal(OccurrenceStatus.Planned, result.Value!.Status);
        // The row is reused rather than duplicated, exactly as reopening a completed day does.
        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task FailedAndCancelledStayDistinct()
    {
        var failed = await CreateAsync(LocalDate, "Missed run");
        var cancelled = await CreateAsync(LocalDate, "Called off");

        await SetStatusAsync(failed, LocalDate, OccurrenceStatus.Failed);
        await SetStatusAsync(cancelled, LocalDate, OccurrenceStatus.Cancelled);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(
            OccurrenceStatus.Failed,
            day.Occurrences.Single(occurrence => occurrence.Title == "Missed run").Status);
        Assert.Equal(
            OccurrenceStatus.Cancelled,
            day.Occurrences.Single(occurrence => occurrence.Title == "Called off").Status);
    }

    [Fact]
    public async Task OnlyTheFailedDayOfASeriesChanges()
    {
        var created = await CreateAsync(
            LocalDate.AddDays(-2),
            recurrence: new PlanningRecurrenceInput(
                PlanningRecurrenceFrequency.Daily,
                1,
                null,
                null));

        await SetStatusAsync(created, LocalDate.AddDays(-2), OccurrenceStatus.Failed);

        var failedDay = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(-2),
            CancellationToken.None);
        var otherDay = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(-1),
            CancellationToken.None);

        Assert.Equal(OccurrenceStatus.Failed, failedDay.Occurrences.Single().Status);
        Assert.Equal(OccurrenceStatus.Planned, otherDay.Occurrences.Single().Status);
    }

    [Fact]
    public async Task AnotherAccountCannotMarkAnOccurrenceFailed()
    {
        var created = await CreateAsync(LocalDate);

        var result = await service.SetOccurrenceStatusAsync(
            UserB,
            created,
            LocalDate,
            OccurrenceStatus.Failed,
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task AMonthSummaryCountsFailedSeparatelyFromCancelled()
    {
        var failed = await CreateAsync(LocalDate, "Missed run");
        var cancelled = await CreateAsync(LocalDate, "Called off");
        var completed = await CreateAsync(LocalDate, "Done");

        await SetStatusAsync(failed, LocalDate, OccurrenceStatus.Failed);
        await SetStatusAsync(cancelled, LocalDate, OccurrenceStatus.Cancelled);
        await SetStatusAsync(completed, LocalDate, OccurrenceStatus.Completed);

        var month = await service.GetMonthAsync(UserA, 2026, 7, CancellationToken.None);
        var day = month.Value!.Days.Single(summary => summary.Date == LocalDate);

        Assert.Equal(3, day.TotalCount);
        Assert.Equal(1, day.FailedCount);
        Assert.Equal(1, day.CancelledCount);
        Assert.Equal(1, day.CompletedCount);
    }

    private Task<OperationResult<CalendarOccurrenceRecord>> SetStatusAsync(
        Guid itemId,
        DateOnly date,
        OccurrenceStatus status) =>
        service.SetOccurrenceStatusAsync(UserA, itemId, date, status, CancellationToken.None);

    private Task<Guid> CreateAsync(
        DateOnly startDate,
        string title = "Run",
        PlanningRecurrenceInput? recurrence = null) =>
        CreateAsync(service, startDate, title, recurrence);

    private static async Task<Guid> CreateAsync(
        CalendarService target,
        DateOnly startDate,
        string title = "Run",
        PlanningRecurrenceInput? recurrence = null)
    {
        var result = await target.CreateAsync(
            UserA,
            new SavePlanningItemInput(
                title,
                null,
                PlanningItemKind.Task,
                PlanningCategory.Fitness,
                PlanningPriority.Normal,
                startDate,
                null,
                null,
                recurrence),
            CancellationToken.None);

        return result.Value!.Id;
    }

    private static CalendarService Build(
        InMemoryCalendarStore calendarStore,
        string timeZoneId,
        DateTimeOffset utcNow)
    {
        var clock = new FixedClock(utcNow);

        return new CalendarService(
            calendarStore,
            new TimeContextService(
                clock,
                new FixedTimeZoneProfileStore(timeZoneId),
                new LocalTimeService()),
            clock);
    }
}
