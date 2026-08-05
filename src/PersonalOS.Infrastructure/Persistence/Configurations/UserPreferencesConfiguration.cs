using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Users;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="UserPreferences"/> and its relationship to the Identity user.
/// </summary>
/// <remarks>
/// The relationship is configured here so that the domain model stays free of any ASP.NET Core
/// Identity dependency.
/// </remarks>
public sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        // The account identifier is the primary key, so the database enforces at most one
        // preferences record per account.
        builder.HasKey(preferences => preferences.UserId);

        builder.Property(preferences => preferences.UserId)
            .ValueGeneratedNever();

        builder.Property(preferences => preferences.TimeZoneId)
            .HasMaxLength(UserPreferences.TimeZoneIdMaxLength)
            .IsRequired();

        // The visible-hours window and the slot length are display choices. They live beside the
        // time zone because both answer "how does this account want its days shown", and neither
        // belongs anywhere near a calendar item.
        builder.Property(preferences => preferences.CalendarDayStartTime)
            .IsRequired();

        builder.Property(preferences => preferences.CalendarDayEndTime)
            .IsRequired();

        builder.Property(preferences => preferences.CalendarSlotMinutes)
            .IsRequired();

        builder.Property(preferences => preferences.CreatedAtUtc)
            .IsRequired();

        builder.Property(preferences => preferences.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<UserPreferences>(preferences => preferences.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
