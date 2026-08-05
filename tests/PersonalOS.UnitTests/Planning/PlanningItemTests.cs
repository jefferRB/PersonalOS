using PersonalOS.Domain.Planning;

namespace PersonalOS.UnitTests.Planning;

/// <summary>
/// Invariants of the calendar aggregate and its occurrence states.
/// </summary>
public sealed class PlanningItemTests
{
    private static readonly Guid UserId = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly DateOnly LocalDate = new(2026, 7, 30);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    [Fact]
    public void CreateStoresATrimmedTitleAndTheSuppliedInstant()
    {
        var item = Create(title: "  Dentist  ");

        Assert.Equal("Dentist", item.Title);
        Assert.Equal(UtcNow, item.CreatedAtUtc);
        Assert.Equal(UtcNow, item.UpdatedAtUtc);
    }

    [Fact]
    public void CreateRefusesAnEmptyTitle()
    {
        Assert.Throws<ArgumentException>(() => Create(title: "   "));
    }

    [Fact]
    public void CreateRefusesAnItemWithNoOwner()
    {
        Assert.Throws<ArgumentException>(() => PlanningItem.Create(
            Guid.Empty,
            "Dentist",
            null,
            PlanningItemKind.Appointment,
            PlanningCategory.Health,
            PlanningPriority.Normal,
            LocalDate,
            null,
            null,
            PlanningRecurrence.Once(),
            UtcNow));
    }

    [Theory]
    [InlineData("09:00", "10:00", true)]
    [InlineData("09:00", null, true)]
    [InlineData(null, null, true)]
    [InlineData(null, "10:00", false)]
    [InlineData("10:00", "09:00", false)]
    [InlineData("10:00", "10:00", false)]
    public void ATimeRangeIsOnlyValidWhenItOccupiesTime(string? start, string? end, bool expected)
    {
        var startTime = start is null ? (TimeOnly?)null : TimeOnly.Parse(start);
        var endTime = end is null ? (TimeOnly?)null : TimeOnly.Parse(end);

        Assert.Equal(expected, PlanningItem.IsTimeRangeValid(startTime, endTime));
    }

    [Fact]
    public void CreateRefusesAnEndTimeWithoutAStartTime()
    {
        Assert.Throws<ArgumentException>(() => Create(endTime: new TimeOnly(10, 0)));
    }

    [Fact]
    public void CreateRefusesARepeatEndDateBeforeTheStartDate()
    {
        Assert.Throws<ArgumentException>(() => Create(
            recurrence: PlanningRecurrence.Create(
                PlanningRecurrenceFrequency.Daily,
                1,
                LocalDate.AddDays(-1),
                0)));
    }

    [Fact]
    public void UpdateChangesTheWholeSeriesAndStampsTheInstant()
    {
        var item = Create();
        var later = UtcNow.AddHours(2);

        item.Update(
            "Dentist check-up",
            "Bring the referral",
            PlanningItemKind.Appointment,
            PlanningCategory.Health,
            PlanningPriority.High,
            LocalDate.AddDays(1),
            new TimeOnly(9, 0),
            new TimeOnly(9, 30),
            PlanningRecurrence.Once(),
            later);

        Assert.Equal("Dentist check-up", item.Title);
        Assert.Equal(PlanningPriority.High, item.Priority);
        Assert.Equal(LocalDate.AddDays(1), item.StartDate);
        Assert.Equal(later, item.UpdatedAtUtc);
        Assert.Equal(UtcNow, item.CreatedAtUtc);
    }

    [Fact]
    public void AnEditIsAcceptedWhileNoOccurrenceHasBeenActedOn()
    {
        var item = Create(recurrence: Daily());

        var refusal = item.CanApplyEdit(
            LocalDate.AddDays(5),
            PlanningRecurrence.Create(PlanningRecurrenceFrequency.Monthly, 2, null, 0),
            hasOccurrenceStates: false);

        Assert.Equal(PlanningEditRefusal.None, refusal);
    }

    [Fact]
    public void TheRepetitionIsFrozenOnceAnOccurrenceHasBeenActedOn()
    {
        var item = Create(recurrence: Daily());

        var refusal = item.CanApplyEdit(
            LocalDate,
            PlanningRecurrence.Create(PlanningRecurrenceFrequency.Weekly, 1, null, 0),
            hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.PatternLocked, refusal);
    }

    [Fact]
    public void TheStartDateIsFrozenOnceAnOccurrenceHasBeenActedOn()
    {
        var item = Create(recurrence: Daily());

        var refusal = item.CanApplyEdit(
            LocalDate.AddDays(1),
            Daily(),
            hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.StartDateLocked, refusal);
    }

    [Fact]
    public void AnEstablishedSeriesMayBeEndedEarly()
    {
        var item = Create(recurrence: Daily());

        var refusal = item.CanApplyEdit(
            LocalDate,
            PlanningRecurrence.Create(
                PlanningRecurrenceFrequency.Daily,
                1,
                LocalDate.AddDays(10),
                0),
            hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.None, refusal);
    }

    [Fact]
    public void AnEstablishedSeriesMayNotBeExtended()
    {
        var item = Create(recurrence: PlanningRecurrence.Create(
            PlanningRecurrenceFrequency.Daily,
            1,
            LocalDate.AddDays(5),
            0));

        var refusal = item.CanApplyEdit(
            LocalDate,
            PlanningRecurrence.Create(
                PlanningRecurrenceFrequency.Daily,
                1,
                LocalDate.AddDays(50),
                0),
            hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.EndDateMayOnlyBeShortened, refusal);
    }

    [Fact]
    public void AOneOffItemMayStillBeRescheduledAfterBeingCompleted()
    {
        var item = Create();

        // A one-off has no series to protect, and rescheduling a task that was already ticked once
        // is an ordinary thing to want.
        var refusal = item.CanApplyEdit(
            LocalDate.AddDays(3),
            PlanningRecurrence.Once(),
            hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.None, refusal);
    }

    [Fact]
    public void ACompletedOneOffItemCannotBecomeASeries()
    {
        var item = Create();

        var refusal = item.CanApplyEdit(LocalDate, Daily(), hasOccurrenceStates: true);

        Assert.Equal(PlanningEditRefusal.PatternLocked, refusal);
    }

    [Fact]
    public void AnOccurrenceStateStartsWithTheDecisionItRecords()
    {
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            Guid.NewGuid(),
            LocalDate,
            OccurrenceStatus.Completed,
            UtcNow);

        Assert.Equal(OccurrenceStatus.Completed, state.Status);
        Assert.Equal(UtcNow, state.CompletedAtUtc);
        Assert.Equal(UtcNow, state.CreatedAtUtc);
    }

    [Fact]
    public void RepeatingADecisionChangesNothing()
    {
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            Guid.NewGuid(),
            LocalDate,
            OccurrenceStatus.Completed,
            UtcNow);

        var changed = state.SetStatus(OccurrenceStatus.Completed, UtcNow.AddHours(3));

        // A checkbox clicked twice, or a retried request, must not rewrite the completion instant.
        Assert.False(changed);
        Assert.Equal(UtcNow, state.CompletedAtUtc);
        Assert.Equal(UtcNow, state.UpdatedAtUtc);
    }

    [Fact]
    public void ReopeningClearsTheCompletionInstant()
    {
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            Guid.NewGuid(),
            LocalDate,
            OccurrenceStatus.Completed,
            UtcNow);

        var changed = state.SetStatus(OccurrenceStatus.Planned, UtcNow.AddHours(1));

        Assert.True(changed);
        Assert.Equal(OccurrenceStatus.Planned, state.Status);
        Assert.Null(state.CompletedAtUtc);
    }

    [Fact]
    public void CancellingClearsTheCompletionInstant()
    {
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            Guid.NewGuid(),
            LocalDate,
            OccurrenceStatus.Completed,
            UtcNow);

        state.SetStatus(OccurrenceStatus.Cancelled, UtcNow.AddHours(1));

        Assert.Equal(OccurrenceStatus.Cancelled, state.Status);
        Assert.Null(state.CompletedAtUtc);
    }

    [Fact]
    public void MovingAStateToTheSameDayChangesNothing()
    {
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            Guid.NewGuid(),
            LocalDate,
            OccurrenceStatus.Completed,
            UtcNow);

        state.MoveTo(LocalDate, UtcNow.AddHours(1));

        Assert.Equal(UtcNow, state.UpdatedAtUtc);
    }

    private static PlanningRecurrence Daily() =>
        PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

    private static PlanningItem Create(
        string? title = "Dentist",
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        PlanningRecurrence? recurrence = null) =>
        PlanningItem.Create(
            UserId,
            title,
            null,
            PlanningItemKind.Appointment,
            PlanningCategory.Health,
            PlanningPriority.Normal,
            LocalDate,
            startTime,
            endTime,
            recurrence ?? PlanningRecurrence.Once(),
            UtcNow);
}
