namespace PersonalOS.Application.Common;

/// <summary>
/// What happened during an application operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>The operation completed and produced a value.</summary>
    Succeeded,

    /// <summary>The submitted values were rejected.</summary>
    Invalid,

    /// <summary>
    /// The requested resource does not exist for this account.
    /// </summary>
    /// <remarks>
    /// A resource owned by another account also reports this status. Telling one user that
    /// another user's record exists would leak information the API has no reason to reveal.
    /// </remarks>
    NotFound,
}

/// <summary>
/// Outcome of an application operation that can succeed, be rejected, or find nothing.
/// </summary>
/// <typeparam name="TValue">Type produced on success.</typeparam>
/// <remarks>
/// The daily modules all need the same three outcomes, so they share one result type instead of
/// repeating a bespoke class per feature. Field-level messages are keyed by the camel-case name
/// used in the API contract, so a controller can hand them to <c>ValidationProblemDetails</c>
/// without translating anything.
/// </remarks>
public sealed class OperationResult<TValue>
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>();

    private OperationResult(
        OperationStatus status,
        TValue? value,
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        Status = status;
        Value = value;
        ValidationErrors = validationErrors;
    }

    /// <summary>What happened.</summary>
    public OperationStatus Status { get; }

    /// <summary>Value produced when <see cref="Status"/> is <see cref="OperationStatus.Succeeded"/>.</summary>
    public TValue? Value { get; }

    /// <summary>Field-level messages keyed by the camel-case contract field name.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    /// <summary>Whether the operation completed.</summary>
    public bool IsSuccess => Status == OperationStatus.Succeeded;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">Value produced by the operation.</param>
    public static OperationResult<TValue> Success(TValue value) =>
        new(OperationStatus.Succeeded, value, EmptyErrors);

    /// <summary>
    /// Creates a rejected result carrying field-level messages.
    /// </summary>
    /// <param name="validationErrors">Messages keyed by contract field name.</param>
    public static OperationResult<TValue> Invalid(
        IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(OperationStatus.Invalid, default, validationErrors);

    /// <summary>
    /// Creates a rejected result carrying a single field-level message.
    /// </summary>
    /// <param name="field">Camel-case contract field name.</param>
    /// <param name="message">Message shown next to that field.</param>
    public static OperationResult<TValue> Invalid(string field, string message) =>
        Invalid(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>
    /// Creates a result for a resource this account cannot see.
    /// </summary>
    public static OperationResult<TValue> NotFound() =>
        new(OperationStatus.NotFound, default, EmptyErrors);
}
