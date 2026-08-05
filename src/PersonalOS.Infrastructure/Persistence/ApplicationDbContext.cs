using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalOS.Domain.Journal;
using PersonalOS.Domain.Nutrition;
using PersonalOS.Domain.Planning;
using PersonalOS.Domain.Routines;
using PersonalOS.Domain.Study;
using PersonalOS.Domain.Users;
using PersonalOS.Infrastructure.Identity;
using PersonalOS.Infrastructure.Persistence.Configurations;

namespace PersonalOS.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    /// <summary>
    /// Application preferences owned by each account.
    /// </summary>
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    /// <summary>Calendar items each account owns, with the rule for when they happen.</summary>
    public DbSet<PlanningItem> PlanningItems => Set<PlanningItem>();

    /// <summary>Decisions recorded about one occurrence on one local calendar day.</summary>
    public DbSet<PlanningItemOccurrenceState> PlanningItemOccurrenceStates =>
        Set<PlanningItemOccurrenceState>();

    /// <summary>Reusable routines with their recurrence rules.</summary>
    public DbSet<RoutineTemplate> RoutineTemplates => Set<RoutineTemplate>();

    /// <summary>Executions of a routine on one local calendar day.</summary>
    public DbSet<RoutineSession> RoutineSessions => Set<RoutineSession>();

    /// <summary>Daily calorie and macronutrient targets, one per account.</summary>
    public DbSet<NutritionGoal> NutritionGoals => Set<NutritionGoal>();

    /// <summary>Meals recorded against a local calendar day.</summary>
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();

    /// <summary>Subjects and learning projects.</summary>
    public DbSet<StudyProject> StudyProjects => Set<StudyProject>();

    /// <summary>Study recorded against a local calendar day.</summary>
    public DbSet<StudySession> StudySessions => Set<StudySession>();

    /// <summary>Daily reflections. The most sensitive table in PersonalOS.</summary>
    public DbSet<DailyJournalEntry> DailyJournalEntries => Set<DailyJournalEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new UserPreferencesConfiguration());
        builder.ApplyConfiguration(new PlanningItemConfiguration());
        builder.ApplyConfiguration(new PlanningItemOccurrenceStateConfiguration());
        builder.ApplyConfiguration(new RoutineTemplateConfiguration());
        builder.ApplyConfiguration(new RoutineStepConfiguration());
        builder.ApplyConfiguration(new RoutineSessionConfiguration());
        builder.ApplyConfiguration(new RoutineStepResultConfiguration());
        builder.ApplyConfiguration(new NutritionGoalConfiguration());
        builder.ApplyConfiguration(new MealEntryConfiguration());
        builder.ApplyConfiguration(new StudyProjectConfiguration());
        builder.ApplyConfiguration(new StudyResourceConfiguration());
        builder.ApplyConfiguration(new StudySessionConfiguration());
        builder.ApplyConfiguration(new DailyJournalEntryConfiguration());

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .HasFilter("[NormalizedEmail] IS NOT NULL")
                .IsUnique();
        });
    }
}
