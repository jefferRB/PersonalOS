using PersonalOS.Application.Calendar;
using PersonalOS.Application.Common;
using PersonalOS.Application.Time;
using PersonalOS.Domain.Planning;
using PersonalOS.UnitTests.Daily;
using PersonalOS.UnitTests.Time;

namespace PersonalOS.UnitTests.Calendar;

/// <summary>
/// The calendar use cases: month summaries, day agendas, the important window, editing, and
/// recording what the user decided about a day.
/// </summary>
public sealed class CalendarServiceTests
{
    private static readonly Guid UserA = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly Guid UserB = Guid.Parse("2f1c6ba8-4b0e-4b39-9d0a-8f5b3d0a1c77");

    // 19:24 UTC on 30 July is still 13:24 on 30 July in Costa Rica, and 31 July in Madrid. Both
    // matter below.
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);
    private static readonly DateOnly LocalDate = new(2026, 7, 30);

    private readonly InMemoryCalendarStore store = new();
    private readonly CalendarService service;

    public CalendarServiceTests()
    {
        service = Build(store, "America/Costa_Rica");
    }

    [Fact]
    public async Task CreateStoresTheItemForTheAuthenticatedAccount()
    {
        var result = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dentist", result.Value!.Title);
        Assert.Equal(PlanningItemKind.Appointment, result.Value.Kind);
        Assert.False(result.Value.IsRecurrencePatternLocked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRejectsAnUnusableTitleWithAFieldMessage(string? title)
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with { Title = title },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.TitleField));
    }

    [Fact]
    public async Task CreateRejectsAMissingStartDateWithAFieldMessage()
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with { StartDate = null },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.StartDateField));
    }

    [Fact]
    public async Task CreateRejectsAnEndTimeThatIsNotAfterTheStartTime()
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with { StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(9, 0) },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.EndTimeField));
    }

    [Fact]
    public async Task CreateRejectsAnEndTimeWithoutAStartTime()
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with { StartTime = null, EndTime = new TimeOnly(9, 0) },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.EndTimeField));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(400)]
    public async Task CreateRejectsAnIntervalOutsideTheAcceptedRange(int interval)
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    interval,
                    null,
                    null),
            },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.IntervalField));
    }

    [Fact]
    public async Task CreateRejectsARepeatEndDateBeforeTheStartDate()
    {
        var result = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    LocalDate.AddDays(-1),
                    null),
            },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.RecurrenceEndDateField));
    }

    [Fact]
    public async Task AMonthSummaryCountsTheGeneratedOccurrencesOfARepeatingItem()
    {
        await service.CreateAsync(
            UserA,
            Input() with
            {
                Title = "Stretch",
                Kind = PlanningItemKind.Routine,
                StartDate = new DateOnly(2026, 7, 1),
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    new DateOnly(2026, 7, 31),
                    null),
            },
            CancellationToken.None);

        var result = await service.GetMonthAsync(UserA, 2026, 7, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var july = result.Value!.Days
            .Where(day => day.Date.Month == 7)
            .ToList();

        // Nothing was written for any of these days: they are calculated from one row.
        Assert.Equal(31, july.Count);
        Assert.All(july, day => Assert.Equal(1, day.TotalCount));
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task AMonthSummaryCoversTheWholeSixWeekGrid()
    {
        var result = await service.GetMonthAsync(UserA, 2026, 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // The grid starts on the Monday of the week holding 1 July and runs six full weeks, so the
        // trailing days of June and August carry their indicators too.
        Assert.Equal(new DateOnly(2026, 6, 29), result.Value!.FromDate);
        Assert.Equal(new DateOnly(2026, 8, 9), result.Value.ToDate);
    }

    [Fact]
    public async Task AMonthSummaryReportsKindsAndImportanceButNoPrivateText()
    {
        await service.CreateAsync(
            UserA,
            Input() with { Priority = PlanningPriority.High, Description = "Bring the referral" },
            CancellationToken.None);

        var result = await service.GetMonthAsync(UserA, 2026, 7, CancellationToken.None);
        var day = result.Value!.Days.Single(summary => summary.Date == LocalDate);

        Assert.Equal(
            [PlanningItemKind.Appointment],
            day.Kinds.Select(kind => kind.Kind));
        Assert.Equal([1], day.Kinds.Select(kind => kind.Count));
        Assert.True(day.HasHighPriority);
        // The summary type has nowhere to put a title or a description, which is what makes it
        // impossible for a month response to carry them.
        Assert.Equal(1, day.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task AMonthOutsideTheCalendarIsRejected(int month)
    {
        var result = await service.GetMonthAsync(UserA, 2026, month, CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.MonthField));
    }

    [Fact]
    public async Task ADayWithNoRequestedDateUsesTheAccountsSavedTimeZone()
    {
        var day = await service.GetDayAsync(UserA, null, CancellationToken.None);

        // 19:24 UTC is 13:24 on 30 July in Costa Rica.
        Assert.Equal(LocalDate, day.Date);
        Assert.Equal(LocalDate, day.TodayLocalDate);
        Assert.Equal(new TimeOnly(13, 24), day.LocalTimeOfDay);
    }

    [Fact]
    public async Task TheSameInstantIsADifferentLocalDayInADifferentTimeZone()
    {
        var madrid = Build(new InMemoryCalendarStore(), "Europe/Madrid");

        var day = await madrid.GetDayAsync(UserA, null, CancellationToken.None);

        // 19:24 UTC is already 21:24 on 30 July in Madrid, but at 23:24 UTC it would be the 31st.
        // The server decides, never the browser.
        Assert.Equal(LocalDate, day.Date);
        Assert.Equal(new TimeOnly(21, 24), day.LocalTimeOfDay);
    }

    [Fact]
    public async Task ADayListsUntimedOccurrencesBeforeTimedOnes()
    {
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Later", StartTime = new TimeOnly(15, 0) },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Anytime", StartTime = null },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Earlier", StartTime = new TimeOnly(9, 0) },
            CancellationToken.None);

        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(
            ["Anytime", "Earlier", "Later"],
            day.Occurrences.Select(occurrence => occurrence.Title));
    }

    [Fact]
    public async Task TheUpcomingWindowCoversSevenDaysFromTheAccountsCurrentDay()
    {
        var week = await service.GetUpcomingAsync(UserA, null, CancellationToken.None);

        Assert.Equal(LocalDate, week.FromDate);
        Assert.Equal(LocalDate.AddDays(6), week.ToDate);
    }

    [Fact]
    public async Task TheUpcomingWindowIncludesEveryEventAndAppointment()
    {
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Dentist", Kind = PlanningItemKind.Appointment },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Concert", Kind = PlanningItemKind.Event },
            CancellationToken.None);

        var week = await service.GetUpcomingAsync(UserA, null, CancellationToken.None);

        Assert.All(week.Days.Single().Occurrences, occurrence => Assert.True(occurrence.IsImportant));
        Assert.Equal(
            ["Concert", "Dentist"],
            week.Days.Single().Occurrences.Select(occurrence => occurrence.Title).Order());
    }

    [Fact]
    public async Task TheUpcomingWindowMarksATaskOrRoutineImportantOnlyWhenItIsHighPriority()
    {
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Ordinary task", Kind = PlanningItemKind.Task },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with { Title = "Ordinary routine", Kind = PlanningItemKind.Routine },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with
            {
                Title = "Urgent task",
                Kind = PlanningItemKind.Task,
                Priority = PlanningPriority.High,
            },
            CancellationToken.None);
        await service.CreateAsync(
            UserA,
            Input() with
            {
                Title = "Urgent routine",
                Kind = PlanningItemKind.Routine,
                Priority = PlanningPriority.High,
            },
            CancellationToken.None);

        var week = await service.GetUpcomingAsync(UserA, null, CancellationToken.None);
        var occurrences = week.Days.Single().Occurrences;

        // Everything in the window is returned; the server's answer is what the client filters on.
        Assert.Equal(4, occurrences.Count);
        Assert.Equal(
            ["Urgent routine", "Urgent task"],
            occurrences
                .Where(occurrence => occurrence.IsImportant)
                .Select(occurrence => occurrence.Title)
                .Order());
    }

    [Fact]
    public async Task TheUpcomingWindowStillReportsACancelledOccurrence()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with { Kind = PlanningItemKind.Appointment },
            CancellationToken.None);

        await service.SetOccurrenceStatusAsync(
            UserA,
            created.Value!.Id,
            LocalDate,
            OccurrenceStatus.Cancelled,
            CancellationToken.None);

        var week = await service.GetUpcomingAsync(UserA, null, CancellationToken.None);
        var occurrence = week.Days.Single().Occurrences.Single();

        // Importance describes the activity, not the day. Hiding a cancelled day is the client's
        // view filter, so the two ideas can be combined rather than silently folded together.
        Assert.Equal(OccurrenceStatus.Cancelled, occurrence.Status);
        Assert.True(occurrence.IsImportant);
    }

    [Fact]
    public async Task NoRowIsWrittenUntilAnOccurrenceIsActedOn()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.Equal(0, store.StateCount);

        await service.SetOccurrenceStatusAsync(
            UserA,
            created.Value!.Id,
            LocalDate,
            OccurrenceStatus.Completed,
            CancellationToken.None);

        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task ReopeningADayNobodyTouchedWritesNothing()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var result = await service.SetOccurrenceStatusAsync(
            UserA,
            created.Value!.Id,
            LocalDate,
            OccurrenceStatus.Planned,
            CancellationToken.None);

        // The absence of a row already means "planned", so recording it would be pure noise.
        Assert.True(result.IsSuccess);
        Assert.Equal(OccurrenceStatus.Planned, result.Value!.Status);
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task CompletingTwiceIsIdempotent()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var first = await Complete(created.Value!.Id);
        var second = await Complete(created.Value.Id);

        Assert.True(second.IsSuccess);
        Assert.Equal(OccurrenceStatus.Completed, second.Value!.Status);
        Assert.Equal(first.Value!.CompletedAtUtc, second.Value.CompletedAtUtc);
        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task CompletingReopeningAndCancellingReuseTheSameRow()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);
        var id = created.Value!.Id;

        await Complete(id);
        await SetStatus(id, OccurrenceStatus.Planned);
        var cancelled = await SetStatus(id, OccurrenceStatus.Cancelled);

        Assert.Equal(OccurrenceStatus.Cancelled, cancelled.Value!.Status);
        Assert.Null(cancelled.Value.CompletedAtUtc);
        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task OnlyTheActedOnDayOfASeriesChanges()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        await Complete(created.Value!.Id);

        var completedDay = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);
        var nextDay = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(1),
            CancellationToken.None);

        Assert.Equal(OccurrenceStatus.Completed, completedDay.Occurrences.Single().Status);
        Assert.Equal(OccurrenceStatus.Planned, nextDay.Occurrences.Single().Status);
    }

    [Fact]
    public async Task ADayTheRuleDoesNotProduceCannotBeActedOn()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var result = await service.SetOccurrenceStatusAsync(
            UserA,
            created.Value!.Id,
            LocalDate.AddDays(1),
            OccurrenceStatus.Completed,
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.OccurrenceDateField));
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task EditingASeriesChangesEveryOccurrence()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        await service.UpdateAsync(
            UserA,
            created.Value!.Id,
            Input() with
            {
                Title = "Renamed",
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        var later = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(4),
            CancellationToken.None);

        Assert.Equal("Renamed", later.Occurrences.Single().Title);
    }

    [Fact]
    public async Task TheRepetitionCannotChangeOnceADayHasBeenActedOn()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        await Complete(created.Value!.Id);

        var result = await service.UpdateAsync(
            UserA,
            created.Value.Id,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Weekly,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.True(result.ValidationErrors.ContainsKey(CalendarService.RecurrenceField));
    }

    [Fact]
    public async Task AnEstablishedSeriesMayStillBeEndedEarly()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        await Complete(created.Value!.Id);

        var result = await service.UpdateAsync(
            UserA,
            created.Value.Id,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    LocalDate.AddDays(2),
                    null),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var beyond = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(5),
            CancellationToken.None);

        Assert.Empty(beyond.Occurrences);
    }

    [Fact]
    public async Task ReschedulingACompletedOneOffMovesItsDecisionWithIt()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);
        await Complete(created.Value!.Id);

        await service.UpdateAsync(
            UserA,
            created.Value.Id,
            Input() with { StartDate = LocalDate.AddDays(3) },
            CancellationToken.None);

        var oldDay = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);
        var newDay = await service.GetDayAsync(
            UserA,
            LocalDate.AddDays(3),
            CancellationToken.None);

        Assert.Empty(oldDay.Occurrences);
        Assert.Equal(OccurrenceStatus.Completed, newDay.Occurrences.Single().Status);
        Assert.Equal(1, store.StateCount);
    }

    [Fact]
    public async Task AnItemReportsWhetherItsRepetitionIsStillOpen()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        var before = await service.GetItemAsync(UserA, created.Value!.Id, CancellationToken.None);
        await Complete(created.Value.Id);
        var after = await service.GetItemAsync(UserA, created.Value.Id, CancellationToken.None);

        Assert.False(before.Value!.IsRecurrencePatternLocked);
        Assert.True(after.Value!.IsRecurrencePatternLocked);
    }

    [Fact]
    public async Task DeletingAnItemRemovesTheWholeSeriesAndItsDecisions()
    {
        var created = await service.CreateAsync(
            UserA,
            Input() with
            {
                Recurrence = new PlanningRecurrenceInput(
                    PlanningRecurrenceFrequency.Daily,
                    1,
                    null,
                    null),
            },
            CancellationToken.None);

        await Complete(created.Value!.Id);

        var deleted = await service.DeleteAsync(UserA, created.Value.Id, CancellationToken.None);
        var day = await service.GetDayAsync(UserA, LocalDate, CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(day.Occurrences);
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task AnotherAccountsItemIsInvisible()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var day = await service.GetDayAsync(UserB, LocalDate, CancellationToken.None);
        var read = await service.GetItemAsync(UserB, created.Value!.Id, CancellationToken.None);
        var deleted = await service.DeleteAsync(UserB, created.Value.Id, CancellationToken.None);

        Assert.Empty(day.Occurrences);
        // Reporting "not found" rather than "forbidden" refuses to confirm that the identifier
        // names something real.
        Assert.Equal(OperationStatus.NotFound, read.Status);
        Assert.False(deleted);
    }

    [Fact]
    public async Task AnotherAccountCannotActOnAnOccurrence()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var result = await service.SetOccurrenceStatusAsync(
            UserB,
            created.Value!.Id,
            LocalDate,
            OccurrenceStatus.Completed,
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Equal(0, store.StateCount);
    }

    [Fact]
    public async Task AnotherAccountCannotEditAnItem()
    {
        var created = await service.CreateAsync(UserA, Input(), CancellationToken.None);

        var result = await service.UpdateAsync(
            UserB,
            created.Value!.Id,
            Input() with { Title = "Hijacked" },
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    private Task<OperationResult<CalendarOccurrenceRecord>> Complete(Guid itemId) =>
        SetStatus(itemId, OccurrenceStatus.Completed);

    private Task<OperationResult<CalendarOccurrenceRecord>> SetStatus(
        Guid itemId,
        OccurrenceStatus status) =>
        service.SetOccurrenceStatusAsync(UserA, itemId, LocalDate, status, CancellationToken.None);

    private static CalendarService Build(InMemoryCalendarStore calendarStore, string timeZoneId)
    {
        var clock = new FixedClock(UtcNow);

        return new CalendarService(
            calendarStore,
            new TimeContextService(
                clock,
                new FixedTimeZoneProfileStore(timeZoneId),
                new LocalTimeService()),
            clock);
    }

    private static SavePlanningItemInput Input() =>
        new(
            "Dentist",
            null,
            PlanningItemKind.Appointment,
            PlanningCategory.Health,
            PlanningPriority.Normal,
            LocalDate,
            null,
            null,
            null);
}
