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
using PersonalOS.Infrastructure.Persistence;

namespace PersonalOS.IntegrationTests;

public sealed class PersonalOSWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? connection;
    private ServiceProvider? sqliteServiceProvider;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            connection?.Dispose();
            sqliteServiceProvider?.Dispose();
        }
    }
}
