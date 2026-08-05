using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Common;
using PersonalOS.Domain.Common;
using PersonalOS.Domain.Routines;

namespace PersonalOS.Application.Routines;

/// <summary>
/// Manages the routines of one account and the sessions that execute them.
/// </summary>
public sealed class RoutineService(IRoutineStore store, IClock clock)
{
    /// <summary>Contract field name used for name validation messages.</summary>
    public const string NameField = "name";

    /// <summary>Contract field name used for description validation messages.</summary>
    public const string DescriptionField = "description";

    /// <summary>Contract field name used for recurrence validation messages.</summary>
    public const string RecurrenceField = "recurrence";

    /// <summary>Contract field name used for interval validation messages.</summary>
    public const string IntervalField = "recurrence.interval";

    /// <summary>Contract field name used for start-date validation messages.</summary>
    public const string StartDateField = "recurrence.startDate";

    /// <summary>Contract field name used for end-date validation messages.</summary>
    public const string EndDateField = "recurrence.endDate";

    /// <summary>Contract field name used for weekday validation messages.</summary>
    public const string SelectedWeekdaysField = "recurrence.selectedWeekdays";

    /// <summary>Contract field name used for step validation messages.</summary>
    public const string StepsField = "steps";

    /// <summary>Contract field name used for step-result validation messages.</summary>
    public const string StepResultsField = "stepResults";

    /// <summary>
    /// Reads the routines of the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="activeOnly">Whether to skip deactivated routines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<RoutineTemplateRecord>> GetTemplatesAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var templates = await store.GetTemplatesAsync(userId, activeOnly, cancellationToken);

        return [.. templates.Select(RoutineTemplateRecord.FromEntity)];
    }

    /// <summary>
    /// Reads one routine owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<RoutineTemplateRecord>> GetTemplateAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await store.FindTemplateAsync(userId, templateId, cancellationToken);

        return template is null
            ? OperationResult<RoutineTemplateRecord>.NotFound()
            : OperationResult<RoutineTemplateRecord>.Success(
                RoutineTemplateRecord.FromEntity(template));
    }

    /// <summary>
    /// Calculates which routines apply inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<RoutineOccurrenceRecord>> GetOccurrencesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var templates = await store.GetTemplatesAsync(userId, activeOnly: true, cancellationToken);
        var sessions = await store.GetSessionsAsync(userId, from, to, cancellationToken);

        return RoutineOccurrenceCalculator.Calculate(templates, sessions, from, to);
    }

    /// <summary>
    /// Creates a routine owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<RoutineTemplateRecord>> CreateAsync(
        Guid userId,
        RoutineTemplateInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var recurrence = ValidateRecurrence(input.Recurrence, errors);
        var steps = ValidateSteps(input.Steps, errors);
        ValidateHeader(input, errors);

        if (errors.HasErrors || recurrence is null)
        {
            return OperationResult<RoutineTemplateRecord>.Invalid(errors.Build());
        }

        var utcNow = clock.UtcNow;
        var template = RoutineTemplate.Create(
            userId,
            input.Name,
            input.Description,
            input.Category,
            recurrence,
            utcNow);
        template.ReplaceSteps(steps, utcNow);

        await store.AddTemplateAsync(template, cancellationToken);

        return OperationResult<RoutineTemplateRecord>.Success(
            RoutineTemplateRecord.FromEntity(template));
    }

    /// <summary>
    /// Edits a routine owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<RoutineTemplateRecord>> UpdateAsync(
        Guid userId,
        Guid templateId,
        RoutineTemplateInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var recurrence = ValidateRecurrence(input.Recurrence, errors);
        var steps = ValidateSteps(input.Steps, errors);
        ValidateHeader(input, errors);

        if (errors.HasErrors || recurrence is null)
        {
            return OperationResult<RoutineTemplateRecord>.Invalid(errors.Build());
        }

        var template = await store.FindTemplateAsync(userId, templateId, cancellationToken);

        if (template is null)
        {
            return OperationResult<RoutineTemplateRecord>.NotFound();
        }

        var utcNow = clock.UtcNow;
        template.Update(
            input.Name,
            input.Description,
            input.Category,
            recurrence,
            input.IsActive,
            utcNow);
        template.ReplaceSteps(steps, utcNow);

        await store.SaveTemplateAsync(template, cancellationToken);

        return OperationResult<RoutineTemplateRecord>.Success(
            RoutineTemplateRecord.FromEntity(template));
    }

    /// <summary>
    /// Deletes a routine owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<bool> DeleteAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken) =>
        store.DeleteTemplateAsync(userId, templateId, cancellationToken);

    /// <summary>
    /// Starts, or returns, the session of a routine on one local calendar day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="templateId">Routine identifier.</param>
    /// <param name="localDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Starting a session twice returns the existing one instead of failing. The user pressed a
    /// button; whether a row already existed is not something they should have to think about.
    /// </remarks>
    public async Task<OperationResult<RoutineSessionRecord>> StartSessionAsync(
        Guid userId,
        Guid templateId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var template = await store.FindTemplateAsync(userId, templateId, cancellationToken);

        if (template is null)
        {
            return OperationResult<RoutineSessionRecord>.NotFound();
        }

        var existing = await store.FindSessionForDateAsync(
            userId,
            templateId,
            localDate,
            cancellationToken);

        if (existing is not null)
        {
            return OperationResult<RoutineSessionRecord>.Success(
                RoutineSessionRecord.FromEntity(existing, template));
        }

        var session = RoutineSession.Start(userId, template, localDate, clock.UtcNow);
        await store.AddSessionAsync(session, cancellationToken);

        return OperationResult<RoutineSessionRecord>.Success(
            RoutineSessionRecord.FromEntity(session, template));
    }

    /// <summary>
    /// Saves progress on a session owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The same call saves partial progress and completes the routine. A user who checks two
    /// exercises and closes the browser keeps those two, because every save is a full save of
    /// what they have entered so far.
    /// </remarks>
    public async Task<OperationResult<RoutineSessionRecord>> SaveSessionAsync(
        Guid userId,
        Guid sessionId,
        RoutineSessionInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var session = await store.FindSessionAsync(userId, sessionId, cancellationToken);

        if (session is null)
        {
            return OperationResult<RoutineSessionRecord>.NotFound();
        }

        var template = await store.FindTemplateAsync(
            userId,
            session.RoutineTemplateId,
            cancellationToken);

        if (template is null)
        {
            return OperationResult<RoutineSessionRecord>.NotFound();
        }

        var errors = ValidateStepResults(input.StepResults, template, session);

        if (errors.HasErrors)
        {
            return OperationResult<RoutineSessionRecord>.Invalid(errors.Build());
        }

        var utcNow = clock.UtcNow;

        foreach (var stepResult in input.StepResults ?? [])
        {
            session.RecordStepResult(
                stepResult.RoutineStepId,
                stepResult.IsCompleted,
                stepResult.ActualSets,
                stepResult.ActualRepetitions,
                stepResult.ActualWeight,
                stepResult.ActualDurationMinutes,
                stepResult.Notes,
                utcNow);
        }

        session.RecordNotes(input.Notes, utcNow);

        if (input.IsCompleted)
        {
            session.Complete(utcNow);
        }
        else
        {
            session.Reopen(utcNow);
        }

        await store.SaveSessionAsync(session, cancellationToken);

        return OperationResult<RoutineSessionRecord>.Success(
            RoutineSessionRecord.FromEntity(session, template));
    }

    /// <summary>
    /// Reads one session owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<RoutineSessionRecord>> GetSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await store.FindSessionAsync(userId, sessionId, cancellationToken);

        if (session is null)
        {
            return OperationResult<RoutineSessionRecord>.NotFound();
        }

        var template = await store.FindTemplateAsync(
            userId,
            session.RoutineTemplateId,
            cancellationToken);

        return template is null
            ? OperationResult<RoutineSessionRecord>.NotFound()
            : OperationResult<RoutineSessionRecord>.Success(
                RoutineSessionRecord.FromEntity(session, template));
    }

    private static void ValidateHeader(
        RoutineTemplateInput input,
        ValidationErrorCollector errors)
    {
        if (!TextRules.TryNormalizeRequired(input.Name, 1, RoutineTemplate.NameMaxLength, out _))
        {
            errors.Add(
                NameField,
                $"Enter a name of {RoutineTemplate.NameMaxLength} characters or fewer.");
        }

        if (!TextRules.TryNormalizeOptional(
            input.Description,
            RoutineTemplate.DescriptionMaxLength,
            out _))
        {
            errors.Add(
                DescriptionField,
                $"The description must be {RoutineTemplate.DescriptionMaxLength} characters or fewer.");
        }
    }

    private static RecurrenceRule? ValidateRecurrence(
        RecurrenceInput? input,
        ValidationErrorCollector errors)
    {
        if (input is null)
        {
            errors.Add(RecurrenceField, "Choose how often this routine repeats.");

            return null;
        }

        if (input.StartDate is null)
        {
            errors.Add(StartDateField, "Choose the day this routine starts.");
        }

        if (!RecurrenceRule.IsIntervalValid(input.Interval))
        {
            errors.Add(
                IntervalField,
                $"The interval must be between {RecurrenceRule.MinInterval} and {RecurrenceRule.MaxInterval}.");
        }

        if (input.StartDate is not null
            && !RecurrenceRule.IsDateRangeValid(input.StartDate.Value, input.EndDate))
        {
            errors.Add(EndDateField, "The end date cannot be before the start date.");
        }

        var mask = input.SelectedWeekdays is null
            ? 0
            : RecurrenceRule.ToMask(input.SelectedWeekdays);

        if (!RecurrenceRule.IsWeekdayMaskValid(input.Frequency, mask))
        {
            errors.Add(SelectedWeekdaysField, "Choose at least one weekday.");
        }

        if (errors.HasErrors || input.StartDate is null)
        {
            return null;
        }

        return RecurrenceRule.Create(
            input.Frequency,
            input.Interval,
            input.StartDate.Value,
            input.EndDate,
            mask);
    }

    private static IReadOnlyList<RoutineStep> ValidateSteps(
        IReadOnlyList<RoutineStepInput>? inputs,
        ValidationErrorCollector errors)
    {
        if (inputs is null || inputs.Count == 0)
        {
            return [];
        }

        if (inputs.Count > RoutineTemplate.MaxSteps)
        {
            errors.Add(
                StepsField,
                $"A routine may hold at most {RoutineTemplate.MaxSteps} steps.");

            return [];
        }

        var steps = new List<RoutineStep>(inputs.Count);

        for (var position = 0; position < inputs.Count; position++)
        {
            var input = inputs[position];
            var stepNumber = position + 1;

            if (!TextRules.TryNormalizeRequired(input.Title, 1, RoutineStep.TitleMaxLength, out _))
            {
                errors.Add(StepsField, $"Step {stepNumber} needs a title.");

                continue;
            }

            if (!RoutineStep.IsCountValid(input.TargetSets)
                || !RoutineStep.IsCountValid(input.TargetRepetitions))
            {
                errors.Add(
                    StepsField,
                    $"Step {stepNumber} must use whole numbers between 1 and {RoutineStep.MaxCount}.");

                continue;
            }

            if (!RoutineStep.IsWeightValid(input.TargetWeight))
            {
                errors.Add(
                    StepsField,
                    $"Step {stepNumber} must use a weight between 0 and {RoutineStep.MaxWeight}.");

                continue;
            }

            if (!RoutineStep.IsDurationValid(input.TargetDurationMinutes))
            {
                errors.Add(
                    StepsField,
                    $"Step {stepNumber} must use a duration between 1 and {RoutineStep.MaxDurationMinutes} minutes.");

                continue;
            }

            if (!TextRules.TryNormalizeOptional(input.Notes, RoutineStep.NotesMaxLength, out _))
            {
                errors.Add(
                    StepsField,
                    $"The note on step {stepNumber} must be {RoutineStep.NotesMaxLength} characters or fewer.");

                continue;
            }

            steps.Add(RoutineStep.Create(
                position,
                input.Title,
                input.StepType,
                input.TargetSets,
                input.TargetRepetitions,
                input.TargetWeight,
                input.TargetDurationMinutes,
                input.Notes));
        }

        return steps;
    }

    private static ValidationErrorCollector ValidateStepResults(
        IReadOnlyList<RoutineStepResultInput>? inputs,
        RoutineTemplate template,
        RoutineSession session)
    {
        var errors = new ValidationErrorCollector();

        if (inputs is null)
        {
            return errors;
        }

        var knownStepIds = template.Steps
            .Select(step => step.Id)
            .Concat(session.StepResults.Select(result => result.RoutineStepId))
            .ToHashSet();

        foreach (var input in inputs)
        {
            // A step identifier that belongs to a different routine must never create a result
            // here, whether it arrived through a mistake or through a crafted request.
            if (!knownStepIds.Contains(input.RoutineStepId))
            {
                errors.Add(StepResultsField, "A step does not belong to this routine.");

                continue;
            }

            if (!RoutineStep.IsCountValid(input.ActualSets)
                || !RoutineStep.IsCountValid(input.ActualRepetitions))
            {
                errors.Add(
                    StepResultsField,
                    $"Sets and repetitions must be whole numbers between 1 and {RoutineStep.MaxCount}.");
            }

            if (!RoutineStep.IsWeightValid(input.ActualWeight))
            {
                errors.Add(
                    StepResultsField,
                    $"The weight must be between 0 and {RoutineStep.MaxWeight}.");
            }

            if (!RoutineStep.IsDurationValid(input.ActualDurationMinutes))
            {
                errors.Add(
                    StepResultsField,
                    $"The duration must be between 1 and {RoutineStep.MaxDurationMinutes} minutes.");
            }

            if (!TextRules.TryNormalizeOptional(input.Notes, RoutineStepResult.NotesMaxLength, out _))
            {
                errors.Add(
                    StepResultsField,
                    $"A step note must be {RoutineStepResult.NotesMaxLength} characters or fewer.");
            }
        }

        return errors;
    }
}
