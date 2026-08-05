using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PersonalOS.Application.Abstractions;
using PersonalOS.Application.Calendar;
using PersonalOS.Application.Journal;
using PersonalOS.Application.Nutrition;
using PersonalOS.Application.Profile;
using PersonalOS.Application.Routines;
using PersonalOS.Application.Study;
using PersonalOS.Application.Time;
using PersonalOS.Application.Today;
using PersonalOS.Infrastructure.Calendar;
using PersonalOS.Infrastructure.Identity;
using PersonalOS.Infrastructure.Journal;
using PersonalOS.Infrastructure.Nutrition;
using PersonalOS.Infrastructure.Persistence;
using PersonalOS.Infrastructure.Profile;
using PersonalOS.Infrastructure.Routines;
using PersonalOS.Infrastructure.Study;
using PersonalOS.Infrastructure.Time;

namespace PersonalOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                    sqlServerOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services
            .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // The API also registers TimeProvider. TryAdd keeps a single source of time whichever
        // composition root runs first.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();

        services.AddSingleton<LocalTimeService>();
        services.AddScoped<IUserProfileStore, UserProfileStore>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<TimeContextService>();

        // Daily operating system. Each module owns one store and one service, and Today composes
        // the services rather than adding a query path of its own.
        services.AddScoped<ICalendarStore, CalendarStore>();
        services.AddScoped<CalendarService>();

        services.AddScoped<IRoutineStore, RoutineStore>();
        services.AddScoped<RoutineService>();

        services.AddScoped<INutritionStore, NutritionStore>();
        services.AddScoped<NutritionService>();

        services.AddScoped<IStudyStore, StudyStore>();
        services.AddScoped<StudyService>();

        services.AddScoped<IJournalStore, JournalStore>();
        services.AddScoped<JournalService>();

        services.AddScoped<TodayService>();

        return services;
    }
}
