using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Planning;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="PlanningItem"/>, including its owned recurrence rule.
/// </summary>
public sealed class PlanningItemConfiguration : IEntityTypeConfiguration<PlanningItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanningItem> builder)
    {
        builder.ToTable("PlanningItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.Title)
            .HasMaxLength(PlanningItem.TitleMaxLength)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasMaxLength(PlanningItem.DescriptionMaxLength);

        builder.Property(item => item.Kind).IsRequired();
        builder.Property(item => item.Category).IsRequired();
        builder.Property(item => item.Priority).IsRequired();
        builder.Property(item => item.StartDate).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();

        // The rule is a value object with no identity of its own, so it lives in the item's own row.
        // A separate table would add a join to a value that is never queried alone.
        builder.OwnsOne(item => item.Recurrence, recurrence =>
        {
            recurrence.Property(rule => rule.Frequency)
                .HasColumnName("RecurrenceFrequency")
                .IsRequired();

            recurrence.Property(rule => rule.Interval)
                .HasColumnName("RecurrenceInterval")
                .IsRequired();

            recurrence.Property(rule => rule.EndDate)
                .HasColumnName("RecurrenceEndDate");

            recurrence.Property(rule => rule.SelectedWeekdaysMask)
                .HasColumnName("RecurrenceSelectedWeekdaysMask")
                .IsRequired();
        });

        builder.Navigation(item => item.Recurrence).IsRequired();

        // Every calendar query asks the same question: "which of this account's series could reach
        // the window between two days?" The composite index answers it without touching another
        // account's rows, and the end date is included so an expired series is skipped by the index
        // rather than by loading it and finding out.
        builder.HasIndex(item => new { item.UserId, item.StartDate })
            .HasDatabaseName("IX_PlanningItems_UserId_StartDate");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="PlanningItemOccurrenceState"/>.
/// </summary>
public sealed class PlanningItemOccurrenceStateConfiguration
    : IEntityTypeConfiguration<PlanningItemOccurrenceState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanningItemOccurrenceState> builder)
    {
        builder.ToTable("PlanningItemOccurrenceStates");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).ValueGeneratedNever();

        builder.Property(state => state.PlanningItemId).IsRequired();
        builder.Property(state => state.OccurrenceDate).IsRequired();
        builder.Property(state => state.Status).IsRequired();
        builder.Property(state => state.CreatedAtUtc).IsRequired();
        builder.Property(state => state.UpdatedAtUtc).IsRequired();

        // One decision per item per local day. Two browser tabs racing to complete the same morning
        // therefore cannot record two different answers for it.
        builder.HasIndex(state => new { state.PlanningItemId, state.OccurrenceDate })
            .HasDatabaseName("IX_PlanningItemOccurrenceStates_PlanningItemId_OccurrenceDate")
            .IsUnique();

        // Reading a month means "every state this account recorded between two days", so the range
        // scan is served straight from this index.
        builder.HasIndex(state => new { state.UserId, state.OccurrenceDate })
            .HasDatabaseName("IX_PlanningItemOccurrenceStates_UserId_OccurrenceDate");

        // The state hangs off its item, and the item hangs off the account. Adding a second cascade
        // path from the account straight to this table would give SQL Server two ways to delete the
        // same row, which it refuses. Ownership is still enforced: every query filters by UserId,
        // and deleting an account still removes these rows through the item.
        builder.HasOne<PlanningItem>()
            .WithMany()
            .HasForeignKey(state => state.PlanningItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
