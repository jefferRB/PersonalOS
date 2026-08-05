using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Common;
using PersonalOS.Application.Time;
using PersonalOS.Domain.Common;
using PersonalOS.Domain.Planning;

namespace PersonalOS.Application.Calendar;

/// <summary>
/// Every calendar use case: reading a month, a day, or the important week, and creating, editing,
/// deleting, and acting on items.
/// </summary>
/// <remarks>
/// <para>
/// Validation lives here so the same rules apply however an item is created, and so they can be
/// tested without an HTTP host. The account identifier is always a parameter supplied by the API
/// from the authentication cookie; this service never reads it from request data.
/// </para>
/// <para>
/// Which day "today" is comes from the account's persisted time zone and the application clock,
/// never from the browser. A client may ask for a specific day, and the service still reports what
/// the account's real current day is so the screen can label it correctly.
/// </para>
/// </remarks>
public sealed class CalendarService(
    ICalendarStore store,
    TimeContextService timeContextService,
    IClock clock)
{
    /// <summary>Contract field name used for title validation messages.</summary>
    public const string TitleField = "title";

    /// <summary>Contract field name used for description validation messages.</summary>
    public const string DescriptionField = "description";

    /// <summary>Contract field name used for start-date validation messages.</summary>
    public const string StartDateField = "startDate";

    /// <summary>Contract field name used for end-time validation messages.</summary>
    public const string EndTimeField = "endTime";

    /// <summary>Contract field name used for interval validation messages.</summary>
    public const string IntervalField = "recurrence.interval";

    /// <summary>Contract field name used for repeat end-date validation messages.</summary>
    public const string RecurrenceEndDateField = "recurrence.endDate";

    /// <summary>Contract field name used for weekday validation messages.</summary>
    public const string SelectedWeekdaysField = "recurrence.selectedWeekdays";

    /// <summary>Contract field name used for recurrence-pattern validation messages.</summary>
    public const string RecurrenceField = "recurrence";

    /// <summary>Contract field name used for month validation messages.</summary>
    public const string MonthField = "month";

    /// <summary>Contract field name used for occurrence-date validation messages.</summary>
    public const string OccurrenceDateField = "occurrenceDate";

    /// <summary>How many days the "next 7 days" section covers, today included.</summary>
    public const int UpcomingWindowDays = 7;

    /// <summary>How many kind indicators a single month cell may advertise.</summary>
    public const int MaxKindIndicatorsPerDay = 3;

    /// <summary>Number of cells in the month grid: six weeks, so the page height never jumps.</summary>
    private const int MonthGridCells = 42;

    /// <summary>
    /// Reads the summaries a month grid needs.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="year">Year being shown.</param>
    /// <param name="month">Month being shown, from 1 to 12.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The window is the six-week grid rather than the calendar month, because the grid shows the
    /// trailing days of the neighbouring months and they must carry their indicators too.
    /// </remarks>
    public async Task<OperationResult<CalendarMonthRecord>> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            return OperationResult<CalendarMonthRecord>.Invalid(
                MonthField,
                "The month must be between 1 and 12.");
        }

        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            return OperationResult<CalendarMonthRecord>.Invalid(
                MonthField,
                "That year is outside the supported range.");
        }

        var localTime = await timeContextService.GetAsync(userId, cancellationToken);
        var firstOfMonth = new DateOnly(year, month, 1);
        var from = StartOfWeek(firstOfMonth);
        var to = from.AddDays(MonthGridCells - 1);

        var occurrences = await ExpandAsync(userId, from, to, cancellationToken);

        return OperationResult<CalendarMonthRecord>.Success(new CalendarMonthRecord(
            year,
            month,
            from,
            to,
            localTime.LocalDate,
            localTime.TimeZoneId,
            OccurrenceExpander.Summarize(occurrences, MaxKindIndicatorsPerDay)));
    }

    /// <summary>
    /// Reads everything on one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="date">
    /// Local calendar day to show. When <see langword="null"/>, the account's current local day is
    /// used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<CalendarDayRecord> GetDayAsync(
        Guid userId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var localTime = await timeContextService.GetAsync(userId, cancellationToken);
        var day = date ?? localTime.LocalDate;
        var occurrences = await ExpandAsync(userId, day, day, cancellationToken);

        return new CalendarDayRecord(
            day,
            localTime.LocalDate,
            localTime.TimeZoneId,
            TimeOnly.FromTimeSpan(localTime.LocalNow.TimeOfDay),
            occurrences);
    }

    /// <summary>
    /// Reads the next seven local days.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="fromDate">
    /// First local calendar day. When <see langword="null"/>, the account's current local day is
    /// used, which is what the calendar page always wants.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Everything in the window is returned. Seven days is a bounded amount of data, and returning
    /// all of it is what lets the section's filters run on the client without a request per click.
    /// </remarks>
    public async Task<UpcomingWeekRecord> GetUpcomingAsync(
        Guid userId,
        DateOnly? fromDate,
        CancellationToken cancellationToken)
    {
        var localTime = await timeContextService.GetAsync(userId, cancellationToken);
        var from = fromDate ?? localTime.LocalDate;
        var to = from.AddDays(UpcomingWindowDays - 1);

        var occurrences = await ExpandAsync(userId, from, to, cancellationToken);

        return new UpcomingWeekRecord(
            from,
            to,
            localTime.LocalDate,
            localTime.TimeZoneId,
            OccurrenceExpander.GroupByDay(occurrences));
    }

    /// <summary>
    /// Expands every item across an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Today calls this rather than owning a query of its own, so both screens read one projection
    /// and there is no second task model to keep in step.
    /// </remarks>
    public Task<IReadOnlyList<CalendarOccurrenceRecord>> GetOccurrencesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        ExpandAsync(userId, from, to, cancellationToken);

    /// <summary>
    /// Reads one item owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<PlanningItemRecord>> GetItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await store.FindItemAsync(userId, itemId, cancellationToken);

        if (item is null)
        {
            return OperationResult<PlanningItemRecord>.NotFound();
        }

        var locked = await IsPatternLockedAsync(userId, item, cancellationToken);

        return OperationResult<PlanningItemRecord>.Success(
            PlanningItemRecord.FromEntity(item, locked));
    }

    /// <summary>
    /// Creates an item owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<PlanningItemRecord>> CreateAsync(
        Guid userId,
        SavePlanningItemInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var recurrence = Validate(input, errors);

        if (errors.HasErrors || recurrence is null)
        {
            return OperationResult<PlanningItemRecord>.Invalid(errors.Build());
        }

        var item = PlanningItem.Create(
            userId,
            input.Title,
            input.Description,
            input.Kind,
            input.Category,
            input.Priority,
            input.StartDate!.Value,
            input.StartTime,
            input.EndTime,
            recurrence,
            clock.UtcNow);

        await store.AddItemAsync(item, cancellationToken);

        return OperationResult<PlanningItemRecord>.Success(
            PlanningItemRecord.FromEntity(item, isRecurrencePatternLocked: false));
    }

    /// <summary>
    /// Edits an item owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Content and times belong to the whole series, so an edit changes every occurrence. Once a day
    /// has been acted on the repetition itself is frozen, because moving it would silently reattach
    /// a completion to a date the user never saw.
    /// </remarks>
    public async Task<OperationResult<PlanningItemRecord>> UpdateAsync(
        Guid userId,
        Guid itemId,
        SavePlanningItemInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var recurrence = Validate(input, errors);

        if (errors.HasErrors || recurrence is null)
        {
            return OperationResult<PlanningItemRecord>.Invalid(errors.Build());
        }

        var item = await store.FindItemAsync(userId, itemId, cancellationToken);

        if (item is null)
        {
            return OperationResult<PlanningItemRecord>.NotFound();
        }

        var hasStates = await store.HasOccurrenceStatesAsync(userId, itemId, cancellationToken);
        var startDate = input.StartDate!.Value;
        var refusal = item.CanApplyEdit(startDate, recurrence, hasStates);

        if (refusal != PlanningEditRefusal.None)
        {
            return OperationResult<PlanningItemRecord>.Invalid(DescribeRefusal(refusal));
        }

        var previousStartDate = item.StartDate;
        var wasOneOff = !item.Recurrence.Repeats;
        var utcNow = clock.UtcNow;

        item.Update(
            input.Title,
            input.Description,
            input.Kind,
            input.Category,
            input.Priority,
            startDate,
            input.StartTime,
            input.EndTime,
            recurrence,
            utcNow);

        // A one-off item that was already acted on may still be rescheduled, and its single state
        // row follows it. Leaving the row behind would strand it on a date the item no longer
        // produces, and the new day would look untouched.
        if (hasStates && wasOneOff && !recurrence.Repeats && startDate != previousStartDate)
        {
            var states = await store.GetStatesForItemAsync(userId, itemId, cancellationToken);

            foreach (var state in states.Where(state => state.OccurrenceDate == previousStartDate))
            {
                state.MoveTo(startDate, utcNow);
            }
        }

        await store.SaveChangesAsync(cancellationToken);

        return OperationResult<PlanningItemRecord>.Success(
            PlanningItemRecord.FromEntity(item, hasStates && recurrence.Repeats));
    }

    /// <summary>
    /// Deletes an item, and with it the whole series and every state recorded against it.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// There is no "delete this occurrence only". Cancelling a single day is what that means, and it
    /// keeps the record of the decision instead of pretending the day never existed.
    /// </remarks>
    public Task<bool> DeleteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        store.DeleteItemAsync(userId, itemId, cancellationToken);

    /// <summary>
    /// Records what the user decided about one occurrence.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="occurrenceDate">Local calendar day.</param>
    /// <param name="status">What the user decided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The call is idempotent: repeating a decision succeeds and changes nothing, so a double click
    /// or a retried request is harmless. Reopening a day nobody ever touched writes no row at all,
    /// because the absence of a row already means exactly that.
    /// </remarks>
    public async Task<OperationResult<CalendarOccurrenceRecord>> SetOccurrenceStatusAsync(
        Guid userId,
        Guid itemId,
        DateOnly occurrenceDate,
        OccurrenceStatus status,
        CancellationToken cancellationToken)
    {
        var item = await store.FindItemAsync(userId, itemId, cancellationToken);

        if (item is null)
        {
            return OperationResult<CalendarOccurrenceRecord>.NotFound();
        }

        // A day the rule does not produce is not an occurrence, whether the client asked by mistake
        // or on purpose. Writing a state for it would invent history the calendar never shows.
        if (!item.OccursOn(occurrenceDate))
        {
            return OperationResult<CalendarOccurrenceRecord>.Invalid(
                OccurrenceDateField,
                "This item does not happen on that day.");
        }

        // Failing to do something is a fact about a day that has already arrived. A future day has
        // not had its chance yet, so recording it as failed would be a claim about something that
        // has not happened. The boundary comes from the account's saved time zone, never the
        // browser's, so a user whose laptop is a day ahead cannot fail tomorrow by accident.
        if (status == OccurrenceStatus.Failed)
        {
            var localTime = await timeContextService.GetAsync(userId, cancellationToken);

            if (occurrenceDate > localTime.LocalDate)
            {
                return OperationResult<CalendarOccurrenceRecord>.Invalid(
                    OccurrenceDateField,
                    "A future activity cannot be marked failed yet.");
            }
        }

        var state = await store.FindStateAsync(userId, itemId, occurrenceDate, cancellationToken);
        var utcNow = clock.UtcNow;

        if (state is null)
        {
            if (status == OccurrenceStatus.Planned)
            {
                return OperationResult<CalendarOccurrenceRecord>.Success(
                    Project(item, occurrenceDate, state: null));
            }

            state = PlanningItemOccurrenceState.Create(
                userId,
                itemId,
                occurrenceDate,
                status,
                utcNow);

            await store.AddStateAsync(state, cancellationToken);
        }
        else if (state.SetStatus(status, utcNow))
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<CalendarOccurrenceRecord>.Success(
            Project(item, occurrenceDate, state));
    }

    private async Task<IReadOnlyList<CalendarOccurrenceRecord>> ExpandAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var items = await store.GetItemsOverlappingAsync(userId, from, to, cancellationToken);
        var states = await store.GetStatesInRangeAsync(userId, from, to, cancellationToken);

        return OccurrenceExpander.Expand(items, states, from, to);
    }

    private async Task<bool> IsPatternLockedAsync(
        Guid userId,
        PlanningItem item,
        CancellationToken cancellationToken) =>
        item.Recurrence.Repeats
        && await store.HasOccurrenceStatesAsync(userId, item.Id, cancellationToken);

    private static CalendarOccurrenceRecord Project(
        PlanningItem item,
        DateOnly occurrenceDate,
        PlanningItemOccurrenceState? state) =>
        new(
            item.Id,
            occurrenceDate,
            item.Title,
            item.Description,
            item.Kind,
            item.Category,
            item.Priority,
            item.StartTime,
            item.EndTime,
            state?.Status ?? OccurrenceStatus.Planned,
            item.Recurrence.Repeats,
            OccurrenceExpander.IsImportant(item.Kind, item.Priority),
            state?.CompletedAtUtc);

    private static IReadOnlyDictionary<string, string[]> DescribeRefusal(
        PlanningEditRefusal refusal) =>
        refusal switch
        {
            PlanningEditRefusal.EndDateBeforeStartDate => Single(
                RecurrenceEndDateField,
                "The repeat end date cannot be before the start date."),
            PlanningEditRefusal.StartDateLocked => Single(
                StartDateField,
                "The start date cannot change once you have completed or cancelled a day. Delete the series and create it again to move it."),
            PlanningEditRefusal.EndDateMayOnlyBeShortened => Single(
                RecurrenceEndDateField,
                "An established series can only be ended earlier, not extended."),
            _ => Single(
                RecurrenceField,
                "How often this repeats cannot change once you have completed or cancelled a day. Delete the series and create it again to change it."),
        };

    private static IReadOnlyDictionary<string, string[]> Single(string field, string message) =>
        new Dictionary<string, string[]> { [field] = [message] };

    /// <summary>
    /// Validates a submission and builds the recurrence rule it describes.
    /// </summary>
    /// <returns>
    /// The rule, or <see langword="null"/> when the submission cannot produce one. The caller checks
    /// the collector rather than relying on the return value alone.
    /// </returns>
    private static PlanningRecurrence? Validate(
        SavePlanningItemInput input,
        ValidationErrorCollector errors)
    {
        if (!TextRules.TryNormalizeRequired(
            input.Title,
            PlanningItem.TitleMinLength,
            PlanningItem.TitleMaxLength,
            out _))
        {
            errors.Add(
                TitleField,
                $"Enter a title of {PlanningItem.TitleMaxLength} characters or fewer.");
        }

        if (!TextRules.TryNormalizeOptional(
            input.Description,
            PlanningItem.DescriptionMaxLength,
            out _))
        {
            errors.Add(
                DescriptionField,
                $"The description must be {PlanningItem.DescriptionMaxLength} characters or fewer.");
        }

        if (input.StartDate is null)
        {
            errors.Add(StartDateField, "Choose the day this starts on.");
        }

        if (!PlanningItem.IsTimeRangeValid(input.StartTime, input.EndTime))
        {
            errors.Add(
                EndTimeField,
                input.StartTime is null
                    ? "Enter a start time before entering an end time."
                    : "The end time must be after the start time.");
        }

        var recurrenceInput = input.Recurrence
            ?? new PlanningRecurrenceInput(PlanningRecurrenceFrequency.None, 1, null, null);

        if (recurrenceInput.Frequency == PlanningRecurrenceFrequency.None)
        {
            return errors.HasErrors ? null : PlanningRecurrence.Once();
        }

        if (!PlanningRecurrence.IsIntervalValid(recurrenceInput.Interval))
        {
            errors.Add(
                IntervalField,
                $"The interval must be between {PlanningRecurrence.MinInterval} and {PlanningRecurrence.MaxInterval}.");
        }

        var mask = recurrenceInput.SelectedWeekdays is null
            ? 0
            : PlanningRecurrence.ToMask(recurrenceInput.SelectedWeekdays);

        if (!PlanningRecurrence.IsWeekdayMaskValid(mask))
        {
            errors.Add(SelectedWeekdaysField, "That weekday selection is not valid.");
        }

        if (input.StartDate is not null
            && !PlanningRecurrence.IsEndDateValid(input.StartDate.Value, recurrenceInput.EndDate))
        {
            errors.Add(
                RecurrenceEndDateField,
                "The repeat end date cannot be before the start date.");
        }

        if (errors.HasErrors)
        {
            return null;
        }

        return PlanningRecurrence.Create(
            recurrenceInput.Frequency,
            recurrenceInput.Interval,
            recurrenceInput.EndDate,
            mask);
    }

    /// <summary>The Monday of the week a local calendar day belongs to.</summary>
    private static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
