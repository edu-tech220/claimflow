using ClaimFlow.Api.Data;
using ClaimFlow.Api.Data.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API pipeline (controllers, auth, middleware) against a
/// file-backed SQLite database instead of SQL Server, seeded with the same
/// demo data used in dev. A real file (rather than a single shared in-memory
/// connection) lets EF Core open independent connections per request, which
/// an in-process ":memory:" connection cannot do under concurrent requests.
/// </summary>
public class ClaimFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"claimflow-tests-{Guid.NewGuid():N}.db");

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

            services.AddDbContext<ClaimFlowDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));

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
            File.Delete(_databasePath);
        }
    }
}
