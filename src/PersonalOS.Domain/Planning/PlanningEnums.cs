namespace PersonalOS.Domain.Planning;

/// <summary>
/// What kind of thing a calendar item is.
/// </summary>
/// <remarks>
/// <para>
/// The kind is a label, not a subtype. Every kind carries the same fields and obeys the same rules,
/// so an inheritance hierarchy would add four classes to express four different words on a chip.
/// </para>
/// <para>
/// <see cref="Routine"/> here means "a repeating thing the user does", which is what the calendar
/// shows. It is unrelated to <c>RoutineTemplate</c>, which records the ordered steps of a workout
/// and its results. A calendar routine has no steps.
/// </para>
/// </remarks>
public enum PlanningItemKind
{
    /// <summary>Something to get done.</summary>
    Task = 0,

    /// <summary>Something the user repeats as part of how they live.</summary>
    Routine = 1,

    /// <summary>Something that happens at a time, with or without other people.</summary>
    Event = 2,

    /// <summary>A commitment made to somebody else at a fixed time.</summary>
    Appointment = 3,
}

/// <summary>
/// Which area of life an item belongs to.
/// </summary>
/// <remarks>
/// The category answers "what part of my life is this", which is a different question from "how
/// much does this matter" (<see cref="PlanningPriority"/>) and from "what sort of thing is this"
/// (<see cref="PlanningItemKind"/>). Keeping the three separate is what lets the calendar colour by
/// kind, badge by priority, and filter by category without any of them fighting for the same pixel.
/// </remarks>
public enum PlanningCategory
{
    /// <summary>Anything that does not belong to a more specific area.</summary>
    General = 0,

    /// <summary>Personal life and errands.</summary>
    Personal = 1,

    /// <summary>Professional work.</summary>
    Work = 2,

    /// <summary>Studying and learning.</summary>
    Study = 3,

    /// <summary>Health, medical care, and recovery.</summary>
    Health = 4,

    /// <summary>Training and physical activity.</summary>
    Fitness = 5,

    /// <summary>Meals, cooking, and food planning.</summary>
    Nutrition = 6,
}

/// <summary>
/// Relative importance of an item.
/// </summary>
public enum PlanningPriority
{
    /// <summary>Can slip without consequence.</summary>
    Low = 0,

    /// <summary>The default.</summary>
    Normal = 1,

    /// <summary>Must not be missed. Shown with an "Important" badge.</summary>
    High = 2,
}

/// <summary>
/// What the user did about one occurrence on one local calendar day.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Planned"/> is the absence of a decision, which is why it is also what an occurrence
/// with no stored state reports. A row is written only once the user actually records an outcome.
/// </para>
/// <para>
/// The numeric values are part of the stored data. New outcomes are appended, never inserted, and
/// an existing value is never reinterpreted: a row written as 2 must still mean cancelled after any
/// future change here.
/// </para>
/// </remarks>
public enum OccurrenceStatus
{
    /// <summary>Nothing has been decided yet. This is the implicit state of every occurrence.</summary>
    Planned = 0,

    /// <summary>The user finished this occurrence.</summary>
    Completed = 1,

    /// <summary>
    /// The user decided this occurrence would not happen, so it stopped being expected.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// The occurrence was expected and did not happen.
    /// </summary>
    /// <remarks>
    /// This is deliberately distinct from <see cref="Cancelled"/>. Calling something off in advance
    /// and failing to do something you meant to do are different facts about a day, and collapsing
    /// them would make an honest record impossible. Nothing marks itself failed: the user says so,
    /// and only about a day that has already arrived.
    /// </remarks>
    Failed = 3,
}

/// <summary>
/// How often a calendar item repeats.
/// </summary>
/// <remarks>
/// The list is deliberately short and covers what a personal planner needs. Positional rules such
/// as "the second Tuesday of the month", exception dates, and the rest of RFC 5545 are not
/// implemented: each one multiplies the cases every query and every screen must handle, and none of
/// them is needed to plan a week.
/// </remarks>
public enum PlanningRecurrenceFrequency
{
    /// <summary>Happens once, on the item's start date.</summary>
    None = 0,

    /// <summary>Repeats every <c>Interval</c> days.</summary>
    Daily = 1,

    /// <summary>Repeats every <c>Interval</c> weeks, on the chosen weekdays.</summary>
    Weekly = 2,

    /// <summary>Repeats every <c>Interval</c> months, on the start date's day of month.</summary>
    Monthly = 3,
}
