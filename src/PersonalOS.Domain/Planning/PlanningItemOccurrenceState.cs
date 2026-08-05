namespace PersonalOS.Domain.Planning;

/// <summary>
/// What the user decided about one occurrence of one calendar item on one local calendar day.
/// </summary>
/// <remarks>
/// <para>
/// This is the only row a repetition ever writes, and it is written the first time the user
/// completes or cancels a specific day. An untouched item costs one row however far the calendar
/// looks ahead, which is what makes calculated occurrences affordable.
/// </para>
/// <para>
/// The absence of a row means <see cref="OccurrenceStatus.Planned"/>. Nothing is written to record
/// that a day is still merely planned, because that is what every day already is.
/// </para>
/// <para>
/// At most one row may exist per item per local day. The database enforces it, so two browser tabs
/// racing to complete the same morning cannot record two different answers.
/// </para>
/// </remarks>
public sealed class PlanningItemOccurrenceState
{
    private PlanningItemOccurrenceState()
    {
    }

    /// <summary>Identifier of this row.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Owning account.
    /// </summary>
    /// <remarks>
    /// The column is carried and indexed here even though the item already identifies the owner, so
    /// every query can filter on it directly instead of joining in order to prove ownership.
    /// </remarks>
    public Guid UserId { get; private set; }

    /// <summary>Calendar item this state belongs to.</summary>
    public Guid PlanningItemId { get; private set; }

    /// <summary>The owner's local calendar day this state describes.</summary>
    public DateOnly OccurrenceDate { get; private set; }

    /// <summary>What the user decided.</summary>
    public OccurrenceStatus Status { get; private set; }

    /// <summary>Instant the row was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Instant the row was last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Instant the occurrence was completed, in UTC, or <see langword="null"/>.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Records a decision about one occurrence for the first time.
    /// </summary>
    /// <param name="userId">Owning account identifier.</param>
    /// <param name="planningItemId">Calendar item the occurrence belongs to.</param>
    /// <param name="occurrenceDate">The owner's local calendar day.</param>
    /// <param name="status">What the user decided.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    public static PlanningItemOccurrenceState Create(
        Guid userId,
        Guid planningItemId,
        DateOnly occurrenceDate,
        OccurrenceStatus status,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (planningItemId == Guid.Empty)
        {
            throw new ArgumentException("An item identifier is required.", nameof(planningItemId));
        }

        var createdAt = utcNow.ToUniversalTime();

        return new PlanningItemOccurrenceState
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanningItemId = planningItemId,
            OccurrenceDate = occurrenceDate,
            Status = status,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            CompletedAtUtc = status == OccurrenceStatus.Completed ? createdAt : null,
        };
    }

    /// <summary>
    /// Applies a decision to an existing row.
    /// </summary>
    /// <param name="status">What the user decided.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <returns><see langword="true"/> when this call changed the row.</returns>
    /// <remarks>
    /// Repeating a decision succeeds without changing anything, so a double click or a retried
    /// request is harmless. Completing an already completed occurrence in particular keeps the
    /// original completion instant: a checkbox clicked twice must not rewrite history.
    /// </remarks>
    public bool SetStatus(OccurrenceStatus status, DateTimeOffset utcNow)
    {
        if (Status == status)
        {
            return false;
        }

        Status = status;
        UpdatedAtUtc = utcNow.ToUniversalTime();
        CompletedAtUtc = status == OccurrenceStatus.Completed ? UpdatedAtUtc : null;

        return true;
    }

    /// <summary>
    /// Moves this state to another local calendar day.
    /// </summary>
    /// <param name="occurrenceDate">New local calendar day.</param>
    /// <param name="utcNow">Current instant supplied by the application clock.</param>
    /// <remarks>
    /// Only a one-off item can be rescheduled once it has been acted on, and its single state row
    /// follows it. Without this the row would be stranded on a date the item no longer produces,
    /// and the rescheduled day would look untouched.
    /// </remarks>
    public void MoveTo(DateOnly occurrenceDate, DateTimeOffset utcNow)
    {
        if (OccurrenceDate == occurrenceDate)
        {
            return;
        }

        OccurrenceDate = occurrenceDate;
        UpdatedAtUtc = utcNow.ToUniversalTime();
    }
}
