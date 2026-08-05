using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PersonalOS.Application.Abstractions;
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.IntegrationTests;

public sealed class PersonalOSWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? connection;
    private ServiceProvider? sqliteServiceProvider;

    /// <summary>
    /// Instant reported to the application under test.
    /// </summary>
    /// <remarks>
    /// The host runs on a controllable clock so that time-dependent assertions never depend on
    /// when the suite executes.
    /// </remarks>
    public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 30, 19, 24, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new TestClock(this));

            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            var dbConnectionDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor is not null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            sqliteServiceProvider = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            services.AddSingleton(connection);
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options
                    .UseSqlite(serviceProvider.GetRequiredService<DbConnection>())
                    .UseInternalServiceProvider(sqliteServiceProvider)
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // SQLite is the behavior-test store; SQL Server migrations are verified separately on LocalDB.
        dbContext.Database.EnsureCreated();

        return host;
    }

    /// <summary>
    /// Deletes the preferences record of an account to simulate a Milestone 1 account that has
    /// not been backfilled.
    /// </summary>
    /// <param name="userId">Account identifier.</param>
    public async Task RemovePreferencesAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.UserPreferences
            .Where(preferences => preferences.UserId == userId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Reports whether an account currently has a preferences record.
    /// </summary>
    /// <param name="userId">Account identifier.</param>
    public async Task<bool> HasPreferencesAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.UserPreferences
            .AsNoTracking()
            .AnyAsync(preferences => preferences.UserId == userId);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            connection?.Dispose();
            sqliteServiceProvider?.Dispose();
        }
    }

    private sealed class TestClock(PersonalOSWebApplicationFactory factory) : IClock
    {
        public DateTimeOffset UtcNow => factory.UtcNow;
    }
}
