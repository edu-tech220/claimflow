using ClaimFlow.Api.Data;
using ClaimFlow.Api.Data.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API pipeline (controllers, auth, middleware) against an in-memory
/// SQLite database instead of SQL Server, seeded with the same demo data used in dev.
/// </summary>
public class ClaimFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ClaimFlowDbContext>) || d.ServiceType == typeof(ClaimFlowDbContext))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<ClaimFlowDbContext>(options => options.UseSqlite(_connection));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ClaimFlowDbContext>();
            db.Database.EnsureCreated();

            var demoAdminOptions = scope.ServiceProvider.GetRequiredService<IOptions<DemoAdminOptions>>();
            DemoDataSeeder.SeedAsync(db, demoAdminOptions, DateTime.UtcNow).GetAwaiter().GetResult();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
