using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Study;

/// <summary>
/// A subject or learning project the user is working through.
/// </summary>
/// <remarks>
/// The project is the aggregate root for its resources. Study sessions reference the project by
/// identifier rather than living inside it, because a session belongs to a day and is queried by
/// date far more often than by project.
/// </remarks>
public sealed class StudyProject
{
    /// <summary>Maximum stored length of the project name.</summary>
    public const int NameMaxLength = 150;

    /// <summary>Maximum stored length of the project description.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>Largest number of resources one project may hold.</summary>
    public const int MaxResources = 50;

    private readonly List<StudyResource> resources = [];

    private StudyProject()
    {
    }

    /// <summary>Identifier of this project.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Subject name, for example <c>Angular</c>.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional longer text.</summary>
    public string? Description { get; private set; }

    /// <summary>Where the project stands.</summary>
    public StudyProjectStatus Status { get; private set; }

    /// <summary>Instant the project was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the project was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Material attached to this project.</summary>
    public IReadOnlyList<StudyResource> Resources => resources;

    /// <summary>
    /// Creates a project owned by one account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="name">Subject name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="status">Where the project stands.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static StudyProject Create(
        Guid userId,
        string? name,
        string? description,
        StudyProjectStatus status,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var createdAt = utcNow.ToUniversalTime();

        return new StudyProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = TextRules.NormalizeRequiredOrThrow(name, 1, NameMaxLength, nameof(name)),
            Description = TextRules.NormalizeOptionalOrThrow(
                description,
                DescriptionMaxLength,
                nameof(description)),
            Status = status,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
        };
    }

    /// <summary>
    /// Applies an edit to the project header.
    /// </summary>
    /// <param name="name">Subject name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="status">Where the project stands.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(
        string? name,
        string? description,
        StudyProjectStatus status,
        DateTimeOffset utcNow)
    {
        Name = TextRules.NormalizeRequiredOrThrow(name, 1, NameMaxLength, nameof(name));
        Description = TextRules.NormalizeOptionalOrThrow(
            description,
            DescriptionMaxLength,
            nameof(description));
        Status = status;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Replaces the attached material with a new list.
    /// </summary>
    /// <param name="newResources">Resources in the order the user arranged them.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void ReplaceResources(
        IReadOnlyList<StudyResource> newResources,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(newResources);

        if (newResources.Count > MaxResources)
        {
            throw new ArgumentException(
                $"A project may hold at most {MaxResources} resources.",
                nameof(newResources));
        }

        resources.Clear();

        foreach (var resource in newResources)
        {
            resource.AttachTo(Id);
            resources.Add(resource);
        }

        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
