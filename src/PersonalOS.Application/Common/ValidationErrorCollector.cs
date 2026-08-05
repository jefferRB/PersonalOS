namespace PersonalOS.Application.Common;

/// <summary>
/// Accumulates field-level validation messages while a service checks a request.
/// </summary>
/// <remarks>
/// Collecting every problem before answering means a user who filled three fields incorrectly
/// sees all three at once instead of discovering them one save at a time.
/// </remarks>
public sealed class ValidationErrorCollector
{
    private readonly Dictionary<string, List<string>> errors = [];

    /// <summary>Whether anything was rejected.</summary>
    public bool HasErrors => errors.Count > 0;

    /// <summary>
    /// Records a message for a field.
    /// </summary>
    /// <param name="field">Camel-case contract field name.</param>
    /// <param name="message">Message shown next to that field.</param>
    public void Add(string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    /// <summary>
    /// Records a message only when a condition holds.
    /// </summary>
    /// <param name="condition">Whether the value is unacceptable.</param>
    /// <param name="field">Camel-case contract field name.</param>
    /// <param name="message">Message shown next to that field.</param>
    public void AddIf(bool condition, string field, string message)
    {
        if (condition)
        {
            Add(field, message);
        }
    }

    /// <summary>
    /// Produces the collected messages in the shape the API contract expects.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Build() =>
        errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
}
