using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Routines;

/// <summary>
/// A reusable set of steps the user repeats on a schedule.
/// </summary>
/// <remarks>
/// <para>
/// A routine template answers "what should happen, and on which days". It is also how PersonalOS
/// represents a repeating calendar activity: the calendar creates a routine whenever the user
/// chooses a repetition, so there is one recurrence engine rather than two.
/// </para>
/// <para>
/// The template is the aggregate root for its steps. Steps are reachable only through the
/// template, which is what keeps their order consistent.
/// </para>
/// </remarks>
public sealed class RoutineTemplate
{
    /// <summary>Maximum stored length of the routine name.</summary>
    public const int NameMaxLength = 150;

    /// <summary>Maximum stored length of the routine description.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>
    /// Largest number of steps one routine may hold.
    /// </summary>
    /// <remarks>
    /// The limit bounds the payload of a single request and the size of the execution screen.
    /// </remarks>
    public const int MaxSteps = 50;

    private readonly List<RoutineStep> steps = [];

    private RoutineTemplate()
    {
    }

    /// <summary>Identifier of this routine.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account. Ownership is assigned once and never changes.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Name shown in lists, for example <c>Monday - Chest</c>.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional longer text.</summary>
    public string? Description { get; private set; }

    /// <summary>What the routine is mostly about.</summary>
    public RoutineCategory Category { get; private set; }

    /// <summary>Which local calendar days this routine applies to.</summary>
    public RecurrenceRule Recurrence { get; private set; } = null!;

    /// <summary>
    /// Whether the routine still appears on Today and on the calendar.
    /// </summary>
    /// <remarks>
    /// Deactivating is preferred over deleting because past sessions stay meaningful only while
    /// their routine still exists.
    /// </remarks>
    public bool IsActive { get; private set; } = true;

    /// <summary>Instant the routine was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the routine was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Ordered steps, from first to last.</summary>
    public IReadOnlyList<RoutineStep> Steps => steps;

    /// <summary>
    /// Creates a routine owned by one account.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="name">Routine name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="category">What the routine is mostly about.</param>
    /// <param name="recurrence">Validated recurrence rule.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static RoutineTemplate Create(
        Guid userId,
        string? name,
        string? description,
        RoutineCategory category,
        RecurrenceRule recurrence,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(recurrence);

        var createdAt = utcNow.ToUniversalTime();

        return new RoutineTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = TextRules.NormalizeRequiredOrThrow(name, 1, NameMaxLength, nameof(name)),
            Description = TextRules.NormalizeOptionalOrThrow(
                description,
                DescriptionMaxLength,
                nameof(description)),
            Category = category,
            Recurrence = recurrence,
            IsActive = true,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
        };
    }

    /// <summary>
    /// Applies an edit to the routine header.
    /// </summary>
    /// <param name="name">Routine name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="category">What the routine is mostly about.</param>
    /// <param name="recurrence">Validated recurrence rule.</param>
    /// <param name="isActive">Whether the routine still appears on Today.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public void Update(
        string? name,
        string? description,
        RoutineCategory category,
        RecurrenceRule recurrence,
        bool isActive,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        Name = TextRules.NormalizeRequiredOrThrow(name, 1, NameMaxLength, nameof(name));
        Description = TextRules.NormalizeOptionalOrThrow(
            description,
            DescriptionMaxLength,
            nameof(description));
        Category = category;
        Recurrence = recurrence;
        IsActive = isActive;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Replaces every step with a new ordered list.
    /// </summary>
    /// <param name="orderedSteps">Steps in the order the user arranged them.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// The editor sends the whole list, so replacing it is both simpler and safer than trying to
    /// reconcile added, moved, and removed steps from partial instructions. Positions are
    /// renumbered here, which is what makes a duplicated or missing order value impossible.
    /// </remarks>
    public void ReplaceSteps(IReadOnlyList<RoutineStep> orderedSteps, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(orderedSteps);

        if (orderedSteps.Count > MaxSteps)
        {
            throw new ArgumentException(
                $"A routine may hold at most {MaxSteps} steps.",
                nameof(orderedSteps));
        }

        steps.Clear();

        for (var position = 0; position < orderedSteps.Count; position++)
        {
            var step = orderedSteps[position];
            step.MoveTo(position);
            step.AttachTo(Id);
            steps.Add(step);
        }

        UpdatedAtUtc = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// Reports whether the routine applies to a local calendar day.
    /// </summary>
    /// <param name="date">Local calendar day being tested.</param>
    /// <remarks>
    /// An inactive routine never applies, so deactivating it removes it from Today and from the
    /// calendar without deleting the history it produced.
    /// </remarks>
    public bool OccursOn(DateOnly date) => IsActive && Recurrence.OccursOn(date);
}
