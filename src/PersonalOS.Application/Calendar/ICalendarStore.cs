using PersonalOS.Domain.Planning;

namespace PersonalOS.Application.Calendar;

/// <summary>
/// Persistence port for calendar items and the days the user acted on.
/// </summary>
/// <remarks>
/// Every member takes the account identifier the API derived from the authenticated principal.
/// Implementations must filter every query by it, so a request can never reach a row that belongs to
/// somebody else, whatever identifier the client sent.
/// </remarks>
public interface ICalendarStore
{
    /// <summary>
    /// Reads the items whose series can produce a day inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The filter is deliberately coarse: an item qualifies when its series could overlap the range
    /// at all. Deciding which days it really produces is the expander's pure calculation, which the
    /// database has no way to express.
    /// </remarks>
    Task<IReadOnlyList<PlanningItem>> GetItemsOverlappingAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the occurrence states recorded inside an inclusive local-date range.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="from">First local calendar day.</param>
    /// <param name="to">Last local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesInRangeAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds one item owned by an account.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The item, or <see langword="null"/> when this account does not own it.</returns>
    Task<PlanningItem?> FindItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reports whether the user has already acted on at least one occurrence of an item.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasOccurrenceStatesAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds the state recorded for one occurrence, if the user has acted on that day.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="occurrenceDate">Local calendar day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PlanningItemOccurrenceState?> FindStateAsync(
        Guid userId,
        Guid itemId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads every state recorded for one item.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PlanningItemOccurrenceState>> GetStatesForItemAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new item.
    /// </summary>
    /// <param name="item">Item created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddItemAsync(PlanningItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new occurrence state.
    /// </summary>
    /// <param name="state">State created by the domain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddStateAsync(PlanningItemOccurrenceState state, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes made to entities previously returned by this store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes one item and every occurrence state it owns.
    /// </summary>
    /// <param name="userId">Account identifier derived from the authenticated principal.</param>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);
}
