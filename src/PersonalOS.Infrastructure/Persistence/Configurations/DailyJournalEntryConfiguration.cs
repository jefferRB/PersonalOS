using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Journal;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="DailyJournalEntry"/>.
/// </summary>
public sealed class DailyJournalEntryConfiguration : IEntityTypeConfiguration<DailyJournalEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DailyJournalEntry> builder)
    {
        builder.ToTable("DailyJournalEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.LocalDate).IsRequired();
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
        builder.Property(entry => entry.UpdatedAtUtc).IsRequired();

        foreach (var section in new[]
        {
            nameof(DailyJournalEntry.WentWell),
            nameof(DailyJournalEntry.WentPoorly),
            nameof(DailyJournalEntry.Cause),
            nameof(DailyJournalEntry.Lesson),
            nameof(DailyJournalEntry.AdjustmentForTomorrow),
            nameof(DailyJournalEntry.FreeNotes),
        })
        {
            builder.Property<string?>(section).HasMaxLength(DailyJournalEntry.SectionMaxLength);
        }

        // One reflection per account per local day. The unique index is what makes repeated saves
        // safe: the second save updates the first entry instead of creating a duplicate day.
        builder.HasIndex(entry => new { entry.UserId, entry.LocalDate })
            .HasDatabaseName("IX_DailyJournalEntries_UserId_LocalDate")
            .IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
