using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Common;
using PersonalOS.Domain.Common;
using PersonalOS.Domain.Study;

namespace PersonalOS.Application.Study;

/// <summary>
/// Manages the study projects of one account and the sessions recorded against them.
/// </summary>
public sealed class StudyService(IStudyStore store, IClock clock)
{
    /// <summary>Contract field name used for project-name validation messages.</summary>
    public const string NameField = "name";

    /// <summary>Contract field name used for description validation messages.</summary>
    public const string DescriptionField = "description";

    /// <summary>Contract field name used for resource validation messages.</summary>
    public const string ResourcesField = "resources";

    /// <summary>Contract field name used for project-reference validation messages.</summary>
    public const string StudyProjectIdField = "studyProjectId";

    /// <summary>Contract field name used for date validation messages.</summary>
    public const string LocalDateField = "localDate";

    /// <summary>Contract field name used for duration validation messages.</summary>
    public const string DurationField = "durationMinutes";

    /// <summary>Contract field name used for summary validation messages.</summary>
    public const string SummaryField = "summary";

    /// <summary>Contract field name used for progress-note validation messages.</summary>
    public const string ProgressNoteField = "progressNote";

    /// <summary>Contract field name used for range validation messages.</summary>
    public const string RangeField = "to";

    /// <summary>Largest range a single session query may cover, in days.</summary>
    public const int MaxRangeDays = 400;

    /// <summary>
    /// Reads the projects of the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<StudyProjectRecord>> GetProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var projects = await store.GetProjectsAsync(userId, cancellationToken);

        return [.. projects.Select(StudyProjectRecord.FromEntity)];
    }

    /// <summary>
    /// Creates a project owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<StudyProjectRecord>> CreateProjectAsync(
        Guid userId,
        StudyProjectInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var resources = ValidateResources(input.Resources, errors);
        ValidateProjectHeader(input, errors);

        if (errors.HasErrors)
        {
            return OperationResult<StudyProjectRecord>.Invalid(errors.Build());
        }

        var utcNow = clock.UtcNow;
        var project = StudyProject.Create(
            userId,
            input.Name,
            input.Description,
            input.Status,
            utcNow);
        project.ReplaceResources(resources, utcNow);

        await store.AddProjectAsync(project, cancellationToken);

        return OperationResult<StudyProjectRecord>.Success(StudyProjectRecord.FromEntity(project));
    }

    /// <summary>
    /// Edits a project owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<StudyProjectRecord>> UpdateProjectAsync(
        Guid userId,
        Guid projectId,
        StudyProjectInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new ValidationErrorCollector();
        var resources = ValidateResources(input.Resources, errors);
        ValidateProjectHeader(input, errors);

        if (errors.HasErrors)
        {
            return OperationResult<StudyProjectRecord>.Invalid(errors.Build());
        }

        var project = await store.FindProjectAsync(userId, projectId, cancellationToken);

        if (project is null)
        {
            return OperationResult<StudyProjectRecord>.NotFound();
        }

        var utcNow = clock.UtcNow;
        project.Update(input.Name, input.Description, input.Status, utcNow);
        project.ReplaceResources(resources, utcNow);

        await store.SaveProjectAsync(project, cancellationToken);

        return OperationResult<StudyProjectRecord>.Success(StudyProjectRecord.FromEntity(project));
    }

    /// <summary>
    /// Reads the sessions recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<IReadOnlyList<StudySessionRecord>>> GetSessionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (to < from)
        {
            return OperationResult<IReadOnlyList<StudySessionRecord>>.Invalid(
                RangeField,
                "The end of the range cannot be before its start.");
        }

        if (to.DayNumber - from.DayNumber > MaxRangeDays)
        {
            return OperationResult<IReadOnlyList<StudySessionRecord>>.Invalid(
                RangeField,
                $"A range may cover at most {MaxRangeDays} days.");
        }

        var sessions = await store.GetSessionsAsync(userId, from, to, cancellationToken);
        var projectNames = await GetProjectNamesAsync(userId, cancellationToken);

        return OperationResult<IReadOnlyList<StudySessionRecord>>.Success(
            [.. sessions.Select(session => StudySessionRecord.FromEntity(
                session,
                projectNames.GetValueOrDefault(session.StudyProjectId, string.Empty)))]);
    }

    /// <summary>
    /// Records a study session owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<StudySessionRecord>> CreateSessionAsync(
        Guid userId,
        StudySessionInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = ValidateSession(input);

        if (errors.HasErrors)
        {
            return OperationResult<StudySessionRecord>.Invalid(errors.Build());
        }

        // The project must belong to the same account. Without this check a crafted request could
        // attach one account's session to another account's project.
        var project = await store.FindProjectAsync(
            userId,
            input.StudyProjectId!.Value,
            cancellationToken);

        if (project is null)
        {
            return OperationResult<StudySessionRecord>.Invalid(
                StudyProjectIdField,
                "Choose one of your study projects.");
        }

        var session = StudySession.Create(
            userId,
            project.Id,
            input.LocalDate!.Value,
            input.StartTime,
            input.DurationMinutes!.Value,
            input.Summary,
            input.ProgressNote,
            clock.UtcNow);

        await store.AddSessionAsync(session, cancellationToken);

        return OperationResult<StudySessionRecord>.Success(
            StudySessionRecord.FromEntity(session, project.Name));
    }

    /// <summary>
    /// Edits a study session owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="input">Submitted values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OperationResult<StudySessionRecord>> UpdateSessionAsync(
        Guid userId,
        Guid sessionId,
        StudySessionInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = ValidateSession(input);

        if (errors.HasErrors)
        {
            return OperationResult<StudySessionRecord>.Invalid(errors.Build());
        }

        var session = await store.FindSessionAsync(userId, sessionId, cancellationToken);

        if (session is null)
        {
            return OperationResult<StudySessionRecord>.NotFound();
        }

        var project = await store.FindProjectAsync(
            userId,
            input.StudyProjectId!.Value,
            cancellationToken);

        if (project is null)
        {
            return OperationResult<StudySessionRecord>.Invalid(
                StudyProjectIdField,
                "Choose one of your study projects.");
        }

        session.Update(
            project.Id,
            input.LocalDate!.Value,
            input.StartTime,
            input.DurationMinutes!.Value,
            input.Summary,
            input.ProgressNote,
            clock.UtcNow);

        await store.SaveSessionAsync(session, cancellationToken);

        return OperationResult<StudySessionRecord>.Success(
            StudySessionRecord.FromEntity(session, project.Name));
    }

    /// <summary>
    /// Deletes a study session owned by the authenticated account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<bool> DeleteSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        store.DeleteSessionAsync(userId, sessionId, cancellationToken);

    private async Task<Dictionary<Guid, string>> GetProjectNamesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // One query for every project name avoids loading a project per session.
        var projects = await store.GetProjectsAsync(userId, cancellationToken);

        return projects.ToDictionary(project => project.Id, project => project.Name);
    }

    private static void ValidateProjectHeader(
        StudyProjectInput input,
        ValidationErrorCollector errors)
    {
        if (!TextRules.TryNormalizeRequired(input.Name, 1, StudyProject.NameMaxLength, out _))
        {
            errors.Add(
                NameField,
                $"Enter a name of {StudyProject.NameMaxLength} characters or fewer.");
        }

        if (!TextRules.TryNormalizeOptional(
            input.Description,
            StudyProject.DescriptionMaxLength,
            out _))
        {
            errors.Add(
                DescriptionField,
                $"The description must be {StudyProject.DescriptionMaxLength} characters or fewer.");
        }
    }

    private static IReadOnlyList<StudyResource> ValidateResources(
        IReadOnlyList<StudyResourceInput>? inputs,
        ValidationErrorCollector errors)
    {
        if (inputs is null || inputs.Count == 0)
        {
            return [];
        }

        if (inputs.Count > StudyProject.MaxResources)
        {
            errors.Add(
                ResourcesField,
                $"A project may hold at most {StudyProject.MaxResources} resources.");

            return [];
        }

        var resources = new List<StudyResource>(inputs.Count);

        for (var position = 0; position < inputs.Count; position++)
        {
            var input = inputs[position];
            var number = position + 1;

            if (!TextRules.TryNormalizeRequired(input.Title, 1, StudyResource.TitleMaxLength, out _))
            {
                errors.Add(ResourcesField, $"Resource {number} needs a title.");

                continue;
            }

            // Only http and https survive this check, so no executable URL scheme can ever be
            // stored and later rendered as a link.
            if (!ExternalUrlRules.IsAcceptable(input.ExternalUrl))
            {
                errors.Add(ResourcesField, $"Resource {number}: {ExternalUrlRules.ValidationMessage}");

                continue;
            }

            if (!TextRules.TryNormalizeOptional(input.Notes, StudyResource.NotesMaxLength, out _))
            {
                errors.Add(
                    ResourcesField,
                    $"The note on resource {number} must be {StudyResource.NotesMaxLength} characters or fewer.");

                continue;
            }

            resources.Add(StudyResource.Create(
                input.Title,
                input.ResourceType,
                input.ExternalUrl,
                input.Notes));
        }

        return resources;
    }

    private static ValidationErrorCollector ValidateSession(StudySessionInput input)
    {
        var errors = new ValidationErrorCollector();

        if (input.StudyProjectId is null || input.StudyProjectId.Value == Guid.Empty)
        {
            errors.Add(StudyProjectIdField, "Choose the project you studied.");
        }

        if (input.LocalDate is null)
        {
            errors.Add(LocalDateField, "Choose the day you studied.");
        }

        if (input.DurationMinutes is null)
        {
            errors.Add(DurationField, "Enter how many minutes you studied.");
        }
        else if (!StudySession.IsDurationValid(input.DurationMinutes.Value))
        {
            errors.Add(
                DurationField,
                $"Enter a whole number of minutes between {StudySession.MinDurationMinutes} and {StudySession.MaxDurationMinutes}.");
        }

        if (!TextRules.TryNormalizeOptional(input.Summary, StudySession.SummaryMaxLength, out _))
        {
            errors.Add(
                SummaryField,
                $"The summary must be {StudySession.SummaryMaxLength} characters or fewer.");
        }

        if (!TextRules.TryNormalizeOptional(
            input.ProgressNote,
            StudySession.ProgressNoteMaxLength,
            out _))
        {
            errors.Add(
                ProgressNoteField,
                $"The progress note must be {StudySession.ProgressNoteMaxLength} characters or fewer.");
        }

        return errors;
    }
}
