using PersonalOS.Application.Calendar;
using PersonalOS.Domain.Planning;

namespace PersonalOS.UnitTests.Calendar;

/// <summary>
/// The pure projection from items and decisions to what a screen renders.
/// </summary>
/// <remarks>
/// Nothing here touches a database, a clock, or a host, which is what lets the recurrence and
/// summary rules be pinned down exhaustively and cheaply.
/// </remarks>
public sealed class OccurrenceExpanderTests
{
    private static readonly Guid UserId = Guid.Parse("8d241a6f-9a79-4d2f-83a4-1377c6d56f52");
    private static readonly DateOnly LocalDate = new(2026, 7, 30);
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    [Fact]
    public void AnOccurrenceWithNoStoredDecisionIsPlanned()
    {
        var item = Item();

        var occurrences = OccurrenceExpander.Expand([item], [], LocalDate, LocalDate);

        Assert.Equal(OccurrenceStatus.Planned, occurrences.Single().Status);
        Assert.Null(occurrences.Single().CompletedAtUtc);
    }

    [Fact]
    public void AStoredDecisionIsAppliedToItsOwnDayOnly()
    {
        var item = Item(recurrence: Daily());
        var state = PlanningItemOccurrenceState.Create(
            UserId,
            item.Id,
            LocalDate.AddDays(1),
            OccurrenceStatus.Completed,
            UtcNow);

        var occurrences = OccurrenceExpander.Expand(
            [item],
            [state],
            LocalDate,
            LocalDate.AddDays(2));

        Assert.Equal(
            [OccurrenceStatus.Planned, OccurrenceStatus.Completed, OccurrenceStatus.Planned],
            occurrences.Select(occurrence => occurrence.Status));
    }

    [Fact]
    public void AnInvertedWindowProducesNothing()
    {
        var occurrences = OccurrenceExpander.Expand(
            [Item(recurrence: Daily())],
            [],
            LocalDate,
            LocalDate.AddDays(-1));

        Assert.Empty(occurrences);
    }

    [Fact]
    public void ASummaryCountsCompletedAndCancelledSeparately()
    {
        var completed = Item(title: "Completed");
        var cancelled = Item(title: "Cancelled");
        var planned = Item(title: "Planned");

        var occurrences = OccurrenceExpander.Expand(
            [completed, cancelled, planned],
            [
                State(completed.Id, OccurrenceStatus.Completed),
                State(cancelled.Id, OccurrenceStatus.Cancelled),
            ],
            LocalDate,
            LocalDate);

        var summary = OccurrenceExpander.Summarize(occurrences, 3).Single();

        Assert.Equal(3, summary.TotalCount);
        Assert.Equal(1, summary.CompletedCount);
        Assert.Equal(1, summary.CancelledCount);
    }

    [Fact]
    public void ASummaryLeavesACancelledKindOutOfTheIndicators()
    {
        var cancelled = Item(title: "Concert", kind: PlanningItemKind.Event);
        var planned = Item(title: "Email", kind: PlanningItemKind.Task);

        var occurrences = OccurrenceExpander.Expand(
            [cancelled, planned],
            [State(cancelled.Id, OccurrenceStatus.Cancelled)],
            LocalDate,
            LocalDate);

        var summary = OccurrenceExpander.Summarize(occurrences, 3).Single();

        // The day still counts three things, but a cancelled event should not advertise itself as
        // an event still happening.
        Assert.Equal([PlanningItemKind.Task], summary.Kinds.Select(kind => kind.Kind));
    }

    [Fact]
    public void ASummaryCapsHowManyKindIndicatorsACellAdvertises()
    {
        var items = new[]
        {
            Item(title: "A", kind: PlanningItemKind.Task),
            Item(title: "B", kind: PlanningItemKind.Routine),
            Item(title: "C", kind: PlanningItemKind.Event),
            Item(title: "D", kind: PlanningItemKind.Appointment),
        };

        var occurrences = OccurrenceExpander.Expand(items, [], LocalDate, LocalDate);
        var summary = OccurrenceExpander.Summarize(occurrences, 3).Single();

        Assert.Equal(3, summary.Kinds.Count);
        Assert.Equal(4, summary.TotalCount);
    }

    [Fact]
    public void ASummaryReportsImportanceOnlyWhileSomethingImportantIsStillHappening()
    {
        var important = Item(priority: PlanningPriority.High);

        var stillOn = OccurrenceExpander.Summarize(
            OccurrenceExpander.Expand([important], [], LocalDate, LocalDate),
            3).Single();

        var calledOff = OccurrenceExpander.Summarize(
            OccurrenceExpander.Expand(
                [important],
                [State(important.Id, OccurrenceStatus.Cancelled)],
                LocalDate,
                LocalDate),
            3).Single();

        Assert.True(stillOn.HasHighPriority);
        Assert.False(calledOff.HasHighPriority);
    }

    [Fact]
    public void DaysAreGroupedInDateOrder()
    {
        var appointment = Item(title: "Dentist", kind: PlanningItemKind.Appointment);
        var ordinary = Item(
            title: "Email",
            kind: PlanningItemKind.Task,
            startDate: LocalDate.AddDays(1));

        var occurrences = OccurrenceExpander.Expand(
            [appointment, ordinary],
            [],
            LocalDate,
            LocalDate.AddDays(6));

        var days = OccurrenceExpander.GroupByDay(occurrences);

        // Both days are returned now: the section filters on the client, so the data it needs to
        // filter has to reach it. The importance flag is what the default view keys off.
        Assert.Equal(2, days.Count);
        Assert.Equal(LocalDate, days[0].Date);
        Assert.True(days[0].Occurrences.Single().IsImportant);
        Assert.False(days[1].Occurrences.Single().IsImportant);
    }

    private static PlanningRecurrence Daily() =>
        PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

    private static PlanningItemOccurrenceState State(Guid itemId, OccurrenceStatus status) =>
        PlanningItemOccurrenceState.Create(UserId, itemId, LocalDate, status, UtcNow);

    private static PlanningItem Item(
        string title = "Dentist",
        PlanningItemKind kind = PlanningItemKind.Appointment,
        PlanningPriority priority = PlanningPriority.Normal,
        DateOnly? startDate = null,
        PlanningRecurrence? recurrence = null) =>
        PlanningItem.Create(
            UserId,
            title,
            null,
            kind,
            PlanningCategory.General,
            priority,
            startDate ?? LocalDate,
            null,
            null,
            recurrence ?? PlanningRecurrence.Once(),
            UtcNow);
}
