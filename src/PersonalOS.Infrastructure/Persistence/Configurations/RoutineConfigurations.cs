using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalOS.Domain.Routines;
using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="RoutineTemplate"/>, including its recurrence rule and its ordered steps.
/// </summary>
public sealed class RoutineTemplateConfiguration : IEntityTypeConfiguration<RoutineTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoutineTemplate> builder)
    {
        builder.ToTable("RoutineTemplates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).ValueGeneratedNever();

        builder.Property(template => template.Name)
            .HasMaxLength(RoutineTemplate.NameMaxLength)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasMaxLength(RoutineTemplate.DescriptionMaxLength);

        builder.Property(template => template.Category).IsRequired();
        builder.Property(template => template.IsActive).IsRequired();
        builder.Property(template => template.CreatedAtUtc).IsRequired();
        builder.Property(template => template.UpdatedAtUtc).IsRequired();

        // The rule is a value object with no identity of its own, so it lives in the routine's own
        // row. A separate table would add a join to a value that is never queried alone.
        builder.OwnsOne(template => template.Recurrence, recurrence =>
        {
            recurrence.Property(rule => rule.Frequency)
                .HasColumnName("RecurrenceFrequency")
                .IsRequired();

            recurrence.Property(rule => rule.Interval)
                .HasColumnName("RecurrenceInterval")
                .IsRequired();

            recurrence.Property(rule => rule.StartDate)
                .HasColumnName("RecurrenceStartDate")
                .IsRequired();

            recurrence.Property(rule => rule.EndDate)
                .HasColumnName("RecurrenceEndDate");

            recurrence.Property(rule => rule.SelectedWeekdaysMask)
                .HasColumnName("RecurrenceSelectedWeekdaysMask")
                .IsRequired();
        });

        builder.Navigation(template => template.Recurrence).IsRequired();

        builder.HasMany(template => template.Steps)
            .WithOne()
            .HasForeignKey(step => step.RoutineTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(template => template.Steps)
            .HasField("steps")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Today and the calendar only ever ask for active routines.
        builder.HasIndex(template => new { template.UserId, template.IsActive })
            .HasDatabaseName("IX_RoutineTemplates_UserId_IsActive");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(template => template.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="RoutineStep"/>.
/// </summary>
public sealed class RoutineStepConfiguration : IEntityTypeConfiguration<RoutineStep>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoutineStep> builder)
    {
        builder.ToTable("RoutineSteps");
        builder.HasKey(step => step.Id);
        builder.Property(step => step.Id).ValueGeneratedNever();

        builder.Property(step => step.Title)
            .HasMaxLength(RoutineStep.TitleMaxLength)
            .IsRequired();

        builder.Property(step => step.Notes)
            .HasMaxLength(RoutineStep.NotesMaxLength);

        builder.Property(step => step.StepType).IsRequired();
        builder.Property(step => step.Order).IsRequired();

        // Weights are money-like: exact values matter, so a decimal is used rather than a float.
        builder.Property(step => step.TargetWeight).HasPrecision(7, 2);

        // The domain renumbers steps on every save, so the database can insist that two steps
        // never claim the same position.
        builder.HasIndex(step => new { step.RoutineTemplateId, step.Order })
            .HasDatabaseName("IX_RoutineSteps_RoutineTemplateId_Order")
            .IsUnique();
    }
}

/// <summary>
/// Maps <see cref="RoutineSession"/>.
/// </summary>
public sealed class RoutineSessionConfiguration : IEntityTypeConfiguration<RoutineSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoutineSession> builder)
    {
        builder.ToTable("RoutineSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();

        builder.Property(session => session.LocalDate).IsRequired();
        builder.Property(session => session.StartedAtUtc).IsRequired();
        builder.Property(session => session.UpdatedAtUtc).IsRequired();

        builder.Property(session => session.Notes)
            .HasMaxLength(RoutineSession.NotesMaxLength);

        builder.HasMany(session => session.StepResults)
            .WithOne()
            .HasForeignKey(result => result.RoutineSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(session => session.StepResults)
            .HasField("stepResults")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // One session per routine per local day. Two tabs racing to start the same routine
        // therefore cannot create two histories for the same morning.
        builder.HasIndex(session => new { session.RoutineTemplateId, session.LocalDate })
            .HasDatabaseName("IX_RoutineSessions_RoutineTemplateId_LocalDate")
            .IsUnique();

        builder.HasIndex(session => new { session.UserId, session.LocalDate })
            .HasDatabaseName("IX_RoutineSessions_UserId_LocalDate");

        // The session hangs off its routine, and the routine hangs off the account. Adding a
        // second cascade path from the account straight to the session would give SQL Server two
        // ways to delete the same row, which it refuses. Ownership is still enforced: every query
        // filters by UserId, and deleting an account still removes these rows through the routine.
        builder.HasOne<RoutineTemplate>()
            .WithMany()
            .HasForeignKey(session => session.RoutineTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="RoutineStepResult"/>.
/// </summary>
public sealed class RoutineStepResultConfiguration : IEntityTypeConfiguration<RoutineStepResult>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoutineStepResult> builder)
    {
        builder.ToTable("RoutineStepResults");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).ValueGeneratedNever();

        builder.Property(result => result.IsCompleted).IsRequired();
        builder.Property(result => result.ActualWeight).HasPrecision(7, 2);

        builder.Property(result => result.Notes)
            .HasMaxLength(RoutineStepResult.NotesMaxLength);

        builder.HasIndex(result => new { result.RoutineSessionId, result.RoutineStepId })
            .HasDatabaseName("IX_RoutineStepResults_RoutineSessionId_RoutineStepId")
            .IsUnique();

        // RoutineStepId deliberately carries no foreign key. Removing an exercise from a routine
        // next month must not erase the weight that was actually lifted last week, and a cascade
        // from RoutineSteps would do exactly that.
        builder.Property(result => result.RoutineStepId).IsRequired();
    }
}
