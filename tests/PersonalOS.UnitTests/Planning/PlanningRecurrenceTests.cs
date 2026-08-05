using PersonalOS.Domain.Planning;

namespace PersonalOS.UnitTests.Planning;

/// <summary>
/// Which local calendar days a rule produces.
/// </summary>
/// <remarks>
/// Every case here is a pure calculation, so none of it depends on a clock, a database, or the
/// machine's time zone. That is the point of keeping recurrence out of the database.
/// </remarks>
public sealed class PlanningRecurrenceTests
{
    // A Thursday, which is what makes the weekly cases readable.
    private static readonly DateOnly Start = new(2026, 7, 30);

    [Fact]
    public void ANonRepeatingRuleProducesOnlyItsStartDate()
    {
        var rule = PlanningRecurrence.Once();

        Assert.Equal([Start], rule.OccurrencesBetween(Start, Start.AddDays(-5), Start.AddDays(30)));
    }

    [Fact]
    public void ADailyRuleProducesEveryDay()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

        var days = rule.OccurrencesBetween(Start, Start, Start.AddDays(3)).ToList();

        Assert.Equal(
            [Start, Start.AddDays(1), Start.AddDays(2), Start.AddDays(3)],
            days);
    }

    [Fact]
    public void ADailyIntervalSkipsTheDaysBetween()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 3, null, 0);

        var days = rule.OccurrencesBetween(Start, Start, Start.AddDays(9)).ToList();

        Assert.Equal(
            [Start, Start.AddDays(3), Start.AddDays(6), Start.AddDays(9)],
            days);
    }

    [Fact]
    public void AWeeklyRuleWithNoWeekdayChosenFollowsItsOwnStartWeekday()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Weekly, 1, null, 0);

        var days = rule.OccurrencesBetween(Start, Start, Start.AddDays(21)).ToList();

        Assert.All(days, day => Assert.Equal(DayOfWeek.Thursday, day.DayOfWeek));
        Assert.Equal(4, days.Count);
    }

    [Fact]
    public void AWeeklyRuleProducesEveryChosenWeekday()
    {
        var rule = PlanningRecurrence.Create(
            PlanningRecurrenceFrequency.Weekly,
            1,
            null,
            PlanningRecurrence.ToMask([DayOfWeek.Monday, DayOfWeek.Friday]));

        var days = rule.OccurrencesBetween(Start, Start, Start.AddDays(13)).ToList();

        // Thursday 30 July: the Friday of the same week, then the Monday and Friday of the next.
        Assert.Equal(
            [new(2026, 7, 31), new(2026, 8, 3), new(2026, 8, 7), new(2026, 8, 10)],
            days);
    }

    [Fact]
    public void AFortnightlyRuleKeepsBothChosenWeekdaysInsideTheSameCycle()
    {
        var rule = PlanningRecurrence.Create(
            PlanningRecurrenceFrequency.Weekly,
            2,
            null,
            PlanningRecurrence.ToMask([DayOfWeek.Monday, DayOfWeek.Wednesday]));

        // Monday 3 August starts the series.
        var start = new DateOnly(2026, 8, 3);
        var days = rule.OccurrencesBetween(start, start, start.AddDays(20)).ToList();

        // Weeks are anchored to Monday, so Monday and Wednesday belong to the same repetition
        // rather than being split across two cycles.
        Assert.Equal(
            [new(2026, 8, 3), new(2026, 8, 5), new(2026, 8, 17), new(2026, 8, 19)],
            days);
    }

    [Fact]
    public void AMonthlyRuleRepeatsOnTheStartDayOfMonth()
    {
        var start = new DateOnly(2026, 1, 15);
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Monthly, 1, null, 0);

        var days = rule.OccurrencesBetween(start, start, new DateOnly(2026, 4, 30)).ToList();

        Assert.Equal(
            [new(2026, 1, 15), new(2026, 2, 15), new(2026, 3, 15), new(2026, 4, 15)],
            days);
    }

    [Fact]
    public void AMonthlyRuleSkipsMonthsThatHaveNoSuchDay()
    {
        var start = new DateOnly(2026, 1, 31);
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Monthly, 1, null, 0);

        var days = rule.OccurrencesBetween(start, start, new DateOnly(2026, 5, 31)).ToList();

        // February and April have no 31st. The occurrence is skipped rather than moved: a bill due
        // on the 31st is not due on the 28th, and silently moving it would be a lie about the date.
        Assert.Equal(
            [new(2026, 1, 31), new(2026, 3, 31), new(2026, 5, 31)],
            days);
    }

    [Fact]
    public void AMonthlyIntervalSkipsTheMonthsBetween()
    {
        var start = new DateOnly(2026, 1, 10);
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Monthly, 3, null, 0);

        var days = rule.OccurrencesBetween(start, start, new DateOnly(2026, 12, 31)).ToList();

        Assert.Equal(
            [new(2026, 1, 10), new(2026, 4, 10), new(2026, 7, 10), new(2026, 10, 10)],
            days);
    }

    [Fact]
    public void AnEndDateStopsTheSeriesOnThatDay()
    {
        var rule = PlanningRecurrence.Create(
            PlanningRecurrenceFrequency.Daily,
            1,
            Start.AddDays(2),
            0);

        var days = rule.OccurrencesBetween(Start, Start, Start.AddDays(10)).ToList();

        // The end date is inclusive: a series that runs "until Saturday" happens on Saturday.
        Assert.Equal([Start, Start.AddDays(1), Start.AddDays(2)], days);
    }

    [Fact]
    public void NothingIsProducedBeforeTheStartDate()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

        Assert.Empty(rule.OccurrencesBetween(Start, Start.AddDays(-10), Start.AddDays(-1)));
    }

    [Fact]
    public void ExpansionNeverLeavesTheRequestedWindow()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

        var days = rule.OccurrencesBetween(Start, Start.AddDays(2), Start.AddDays(4)).ToList();

        Assert.Equal([Start.AddDays(2), Start.AddDays(3), Start.AddDays(4)], days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void AnIntervalOutsideTheAcceptedRangeIsRefused(int interval)
    {
        Assert.False(PlanningRecurrence.IsIntervalValid(interval));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, interval, null, 0));
    }

    [Fact]
    public void AnEndDateBeforeTheStartDateIsRefused()
    {
        Assert.False(PlanningRecurrence.IsEndDateValid(Start, Start.AddDays(-1)));
        Assert.True(PlanningRecurrence.IsEndDateValid(Start, Start));
    }

    [Fact]
    public void WeekdaysAreOnlyKeptForAWeeklyRule()
    {
        var mask = PlanningRecurrence.ToMask([DayOfWeek.Monday]);
        var daily = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, mask);

        // Clearing the mask keeps stored rules comparable and stops a hidden weekday from
        // reappearing if the user switches back to weekly later.
        Assert.Equal(0, daily.SelectedWeekdaysMask);
    }

    [Fact]
    public void AMaskRoundTripsThroughItsWeekdays()
    {
        var weekdays = new[] { DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Saturday };

        var roundTripped = PlanningRecurrence.FromMask(PlanningRecurrence.ToMask(weekdays));

        Assert.Equal(weekdays, roundTripped);
    }

    [Fact]
    public void APatternComparisonIgnoresTheEndDate()
    {
        var open = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 2, null, 0);
        var ending = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 2, Start, 0);
        var different = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 3, null, 0);

        Assert.True(open.HasSamePattern(ending));
        Assert.False(open.HasSamePattern(different));
    }

    [Fact]
    public void AnEstablishedSeriesMayBeShortenedButNotExtended()
    {
        var rule = PlanningRecurrence.Create(
            PlanningRecurrenceFrequency.Daily,
            1,
            Start.AddDays(10),
            0);

        Assert.True(rule.AllowsEndDateChangeTo(Start.AddDays(5)));
        Assert.True(rule.AllowsEndDateChangeTo(Start.AddDays(10)));
        Assert.False(rule.AllowsEndDateChangeTo(Start.AddDays(20)));
        Assert.False(rule.AllowsEndDateChangeTo(null));
    }

    [Fact]
    public void AnOpenEndedSeriesMayBeGivenAnEndDate()
    {
        var rule = PlanningRecurrence.Create(PlanningRecurrenceFrequency.Daily, 1, null, 0);

        Assert.True(rule.AllowsEndDateChangeTo(Start.AddDays(5)));
        Assert.True(rule.AllowsEndDateChangeTo(null));
    }
}
