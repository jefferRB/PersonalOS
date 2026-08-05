using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="NutritionGoal"/>.
/// </summary>
public sealed class NutritionGoalConfiguration : IEntityTypeConfiguration<NutritionGoal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NutritionGoal> builder)
    {
        builder.ToTable("NutritionGoals");

        // The account identifier is the primary key, so the database enforces one goal per
        // account without a separate unique index.
        builder.HasKey(goal => goal.UserId);
        builder.Property(goal => goal.UserId).ValueGeneratedNever();

        builder.Property(goal => goal.DailyCalorieTarget).IsRequired();
        builder.Property(goal => goal.ProteinTargetGrams).HasPrecision(7, 2);
        builder.Property(goal => goal.CarbohydrateTargetGrams).HasPrecision(7, 2);
        builder.Property(goal => goal.FatTargetGrams).HasPrecision(7, 2);
        builder.Property(goal => goal.CreatedAtUtc).IsRequired();
        builder.Property(goal => goal.UpdatedAtUtc).IsRequired();

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<NutritionGoal>(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="MealEntry"/>.
/// </summary>
public sealed class MealEntryConfiguration : IEntityTypeConfiguration<MealEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MealEntry> builder)
    {
        builder.ToTable("MealEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.Name)
            .HasMaxLength(MealEntry.NameMaxLength)
            .IsRequired();

        builder.Property(entry => entry.Quantity).HasMaxLength(MealEntry.QuantityMaxLength);
        builder.Property(entry => entry.Notes).HasMaxLength(MealEntry.NotesMaxLength);

        builder.Property(entry => entry.MealType).IsRequired();
        builder.Property(entry => entry.LocalDate).IsRequired();
        builder.Property(entry => entry.Calories).IsRequired();
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
        builder.Property(entry => entry.UpdatedAtUtc).IsRequired();

        // Grams are exact quantities the user typed, so they are stored as decimals rather than
        // as floating-point values that would drift when summed.
        builder.Property(entry => entry.ProteinGrams).HasPrecision(7, 2);
        builder.Property(entry => entry.CarbohydrateGrams).HasPrecision(7, 2);
        builder.Property(entry => entry.FatGrams).HasPrecision(7, 2);

        builder.HasIndex(entry => new { entry.UserId, entry.LocalDate })
            .HasDatabaseName("IX_MealEntries_UserId_LocalDate");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
