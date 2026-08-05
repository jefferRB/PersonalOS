using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Study;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="StudyProject"/> and its resources.
/// </summary>
public sealed class StudyProjectConfiguration : IEntityTypeConfiguration<StudyProject>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyProject> builder)
    {
        builder.ToTable("StudyProjects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).ValueGeneratedNever();

        builder.Property(project => project.Name)
            .HasMaxLength(StudyProject.NameMaxLength)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(StudyProject.DescriptionMaxLength);

        builder.Property(project => project.Status).IsRequired();
        builder.Property(project => project.CreatedAtUtc).IsRequired();
        builder.Property(project => project.UpdatedAtUtc).IsRequired();

        builder.HasMany(project => project.Resources)
            .WithOne()
            .HasForeignKey(resource => resource.StudyProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Resources)
            .HasField("resources")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(project => project.UserId)
            .HasDatabaseName("IX_StudyProjects_UserId");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(project => project.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="StudyResource"/>.
/// </summary>
public sealed class StudyResourceConfiguration : IEntityTypeConfiguration<StudyResource>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyResource> builder)
    {
        builder.ToTable("StudyResources");
        builder.HasKey(resource => resource.Id);
        builder.Property(resource => resource.Id).ValueGeneratedNever();

        builder.Property(resource => resource.Title)
            .HasMaxLength(StudyResource.TitleMaxLength)
            .IsRequired();

        // The stored value is always an absolute http or https URL: the domain rejects every
        // other scheme before it can reach this column.
        builder.Property(resource => resource.ExternalUrl)
            .HasMaxLength(ExternalUrlRules.MaxLength);

        builder.Property(resource => resource.Notes)
            .HasMaxLength(StudyResource.NotesMaxLength);

        builder.Property(resource => resource.ResourceType).IsRequired();
    }
}

/// <summary>
/// Maps <see cref="StudySession"/>.
/// </summary>
public sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("StudySessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();

        builder.Property(session => session.LocalDate).IsRequired();
        builder.Property(session => session.DurationMinutes).IsRequired();
        builder.Property(session => session.CreatedAtUtc).IsRequired();
        builder.Property(session => session.UpdatedAtUtc).IsRequired();

        builder.Property(session => session.Summary)
            .HasMaxLength(StudySession.SummaryMaxLength);

        builder.Property(session => session.ProgressNote)
            .HasMaxLength(StudySession.ProgressNoteMaxLength);

        builder.HasIndex(session => new { session.UserId, session.LocalDate })
            .HasDatabaseName("IX_StudySessions_UserId_LocalDate");

        // As with routine sessions, the row hangs off its project so that the account has exactly
        // one cascade path to it. Ownership is still enforced by filtering every query on UserId.
        builder.HasOne<StudyProject>()
            .WithMany()
            .HasForeignKey(session => session.StudyProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
