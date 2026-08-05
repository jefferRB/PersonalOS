using PersonalOS.Domain.Routines;

namespace PersonalOS.UnitTests.Routines;

/// <summary>
/// Behaviour of the rule that decides which local days a routine applies to.
/// </summary>
/// <remarks>
/// Recurrence is calculated rather than generated, so these tests are the only thing standing
/// between the product and a calendar that quietly shows the wrong days. They use fixed dates and
/// no clock, because the calculation must be pure.
/// </remarks>
public sealed class RecurrenceRuleTests
{
    private static readonly DateOnly Thursday30July2026 = new(2026, 7, 30);

    [Fact]
    public void Once_OccursOnlyOnItsStartDate()
    {
        var rule = RecurrenceRule.Once(Thursday30July2026);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(1)));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(-1)));
    }

    [Fact]
    public void Daily_OccursEveryDayFromTheStart()
    {
        var rule = Create(RecurrenceFrequency.Daily, interval: 1);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(1)));
        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(45)));
    }

    [Fact]
    public void Daily_WithAnIntervalSkipsTheDaysBetween()
    {
        var rule = Create(RecurrenceFrequency.Daily, interval: 3);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(1)));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(2)));
        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(3)));
    }

    [Fact]
    public void NothingOccursBeforeTheStartDate()
    {
        var rule = Create(RecurrenceFrequency.Daily, interval: 1);

        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(-1)));
    }

    [Fact]
    public void Weekly_OccursOnTheSameWeekdayAsTheStart()
    {
        var rule = Create(RecurrenceFrequency.Weekly, interval: 1);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(7)));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(1)));
    }

    [Fact]
    public void Weekly_WithAnIntervalSkipsTheWeeksBetween()
    {
        var rule = Create(RecurrenceFrequency.Weekly, interval: 2);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(7)));
        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(14)));
    }

    [Fact]
    public void SelectedWeekdays_OccursOnEveryChosenDay()
    {
        // Starting on a Thursday but repeating on Monday and Wednesday.
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.SelectedWeekdays,
            interval: 1,
            Thursday30July2026,
            endDate: null,
            RecurrenceRule.ToMask([DayOfWeek.Monday, DayOfWeek.Wednesday]));

        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 3)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 5)));
        Assert.False(rule.OccursOn(new DateOnly(2026, 8, 4)));
        // The start day itself is a Thursday, which is not one of the chosen weekdays.
        Assert.False(rule.OccursOn(Thursday30July2026));
    }

    [Fact]
    public void SelectedWeekdays_WithAnIntervalKeepsAWholeWeekTogether()
    {
        // Every other week on Monday and Wednesday. Both days must fall in the same cycle,
        // which is why weeks are anchored to Monday rather than to the start date.
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.SelectedWeekdays,
            interval: 2,
            new DateOnly(2026, 8, 3),
            endDate: null,
            RecurrenceRule.ToMask([DayOfWeek.Monday, DayOfWeek.Wednesday]));

        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 3)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 5)));
        Assert.False(rule.OccursOn(new DateOnly(2026, 8, 10)));
        Assert.False(rule.OccursOn(new DateOnly(2026, 8, 12)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 17)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 19)));
    }

    [Fact]
    public void Monthly_OccursOnTheSameDayNumber()
    {
        var rule = Create(RecurrenceFrequency.Monthly, interval: 1);

        Assert.True(rule.OccursOn(new DateOnly(2026, 8, 30)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 9, 30)));
        Assert.False(rule.OccursOn(new DateOnly(2026, 8, 29)));
    }

    [Fact]
    public void Monthly_WithAnIntervalSkipsTheMonthsBetween()
    {
        var rule = Create(RecurrenceFrequency.Monthly, interval: 3);

        Assert.True(rule.OccursOn(Thursday30July2026));
        Assert.False(rule.OccursOn(new DateOnly(2026, 8, 30)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 10, 30)));
    }

    [Fact]
    public void Monthly_FromTheThirtyFirstClampsToTheEndOfAShorterMonth()
    {
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.Monthly,
            interval: 1,
            new DateOnly(2026, 1, 31),
            endDate: null,
            selectedWeekdaysMask: 0);

        // February has 28 days in 2026, so the occurrence lands on the last day rather than
        // being skipped entirely or rolling into March.
        Assert.True(rule.OccursOn(new DateOnly(2026, 2, 28)));
        Assert.False(rule.OccursOn(new DateOnly(2026, 3, 1)));
        Assert.True(rule.OccursOn(new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public void Monthly_ClampsToTheLeapDayInALeapYear()
    {
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.Monthly,
            interval: 1,
            new DateOnly(2028, 1, 31),
            endDate: null,
            selectedWeekdaysMask: 0);

        Assert.True(rule.OccursOn(new DateOnly(2028, 2, 29)));
        Assert.False(rule.OccursOn(new DateOnly(2028, 2, 28)));
    }

    [Fact]
    public void AnEndDateStopsTheSeriesAndIncludesTheEndDayItself()
    {
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.Daily,
            interval: 1,
            Thursday30July2026,
            Thursday30July2026.AddDays(2),
            selectedWeekdaysMask: 0);

        Assert.True(rule.OccursOn(Thursday30July2026.AddDays(2)));
        Assert.False(rule.OccursOn(Thursday30July2026.AddDays(3)));
    }

    [Fact]
    public void OccurrencesBetween_ReturnsOnlyTheDaysInsideTheWindow()
    {
        var rule = Create(RecurrenceFrequency.Weekly, interval: 1);

        var occurrences = rule
            .OccurrencesBetween(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
            .ToList();

        Assert.Equal(
            [
                new DateOnly(2026, 8, 6),
                new DateOnly(2026, 8, 13),
                new DateOnly(2026, 8, 20),
                new DateOnly(2026, 8, 27),
            ],
            occurrences);
    }

    [Fact]
    public void OccurrencesBetween_IsEmptyForAWindowEntirelyBeforeTheStart()
    {
        var rule = Create(RecurrenceFrequency.Daily, interval: 1);

        Assert.Empty(rule.OccurrencesBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void AnIntervalOutsideTheAcceptedRangeIsRejected(int interval)
    {
        Assert.False(RecurrenceRule.IsIntervalValid(interval));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Create(RecurrenceFrequency.Daily, interval));
    }

    [Fact]
    public void AnEndDateBeforeTheStartDateIsRejected()
    {
        Assert.False(
            RecurrenceRule.IsDateRangeValid(Thursday30July2026, Thursday30July2026.AddDays(-1)));

        Assert.Throws<ArgumentException>(() => RecurrenceRule.Create(
            RecurrenceFrequency.Daily,
            interval: 1,
            Thursday30July2026,
            Thursday30July2026.AddDays(-1),
            selectedWeekdaysMask: 0));
    }

    [Fact]
    public void SelectedWeekdaysWithNoWeekdayChosenIsRejected()
    {
        // A routine that never happens is never what the user meant.
        Assert.False(
            RecurrenceRule.IsWeekdayMaskValid(RecurrenceFrequency.SelectedWeekdays, 0));

        Assert.Throws<ArgumentException>(() => RecurrenceRule.Create(
            RecurrenceFrequency.SelectedWeekdays,
            interval: 1,
            Thursday30July2026,
            endDate: null,
            selectedWeekdaysMask: 0));
    }

    [Fact]
    public void WeekdaysAreClearedForAFrequencyThatDoesNotUseThem()
    {
        var rule = RecurrenceRule.Create(
            RecurrenceFrequency.Weekly,
            interval: 1,
            Thursday30July2026,
            endDate: null,
            RecurrenceRule.ToMask([DayOfWeek.Monday]));

        // A hidden weekday must not reappear if the frequency is changed back later.
        Assert.Equal(0, rule.SelectedWeekdaysMask);
        Assert.Empty(rule.SelectedWeekdays());
    }

    [Fact]
    public void WeekdaysRoundTripThroughTheStoredBitmask()
    {
        var weekdays = new[] { DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Saturday };
        var mask = RecurrenceRule.ToMask(weekdays);

        Assert.Equal(weekdays.OrderBy(day => (int)day), RecurrenceRule.FromMask(mask));
    }

    [Fact]
    public void TheCalculationIsDeterministic()
    {
        var rule = Create(RecurrenceFrequency.Weekly, interval: 1);

        // The same question must always give the same answer, on any host and at any moment.
        Assert.Equal(rule.OccursOn(Thursday30July2026), rule.OccursOn(Thursday30July2026));
        Assert.Equal(
            rule.OccurrencesBetween(Thursday30July2026, Thursday30July2026.AddDays(90)),
            rule.OccurrencesBetween(Thursday30July2026, Thursday30July2026.AddDays(90)));
    }

    private static RecurrenceRule Create(RecurrenceFrequency frequency, int interval) =>
        RecurrenceRule.Create(
            frequency,
            interval,
            Thursday30July2026,
            endDate: null,
            selectedWeekdaysMask: 0);
}

/// <summary>
/// Convenience for reading the weekdays of a rule in tests.
/// </summary>
internal static class RecurrenceRuleTestExtensions
{
    public static IReadOnlyList<DayOfWeek> SelectedWeekdays(this RecurrenceRule rule) =>
        RecurrenceRule.FromMask(rule.SelectedWeekdaysMask);
}
