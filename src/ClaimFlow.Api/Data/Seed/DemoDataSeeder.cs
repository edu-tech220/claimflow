using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Security;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Data.Seed;

/// <summary>
/// Seeds a demo admin/adjuster/supervisor account plus a couple of active policies so
/// the API and the Razor front end have something to show on first run.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ClaimFlowDbContext db, IOptions<DemoAdminOptions> demoAdminOptions, DateTime nowUtc)
    {
        if (!db.Users.Any())
        {
            var admin = demoAdminOptions.Value;
            var (adminHash, adminSalt) = PasswordHasher.Hash(admin.Password);
            var (adjusterHash, adjusterSalt) = PasswordHasher.Hash("Adjuster#12345");
            var (supervisorHash, supervisorSalt) = PasswordHasher.Hash("Supervisor#12345");

            db.Users.AddRange(
                new User
                {
                    Email = admin.Email,
                    DisplayName = admin.DisplayName,
                    PasswordHash = adminHash,
                    PasswordSalt = adminSalt,
                    Role = UserRole.Admin,
                    CreatedAtUtc = nowUtc
                },
                new User
                {
                    Email = "adjuster@claimflow.dev",
                    DisplayName = "Dana Adjuster",
                    PasswordHash = adjusterHash,
                    PasswordSalt = adjusterSalt,
                    Role = UserRole.Adjuster,
                    CreatedAtUtc = nowUtc
                },
                new User
                {
                    Email = "supervisor@claimflow.dev",
                    DisplayName = "Sam Supervisor",
                    PasswordHash = supervisorHash,
                    PasswordSalt = supervisorSalt,
                    Role = UserRole.Supervisor,
                    CreatedAtUtc = nowUtc
                });
        }

        if (!db.Policies.Any())
        {
            var today = DateOnly.FromDateTime(nowUtc);

            db.Policies.AddRange(
                new Policy
                {
                    PolicyNumber = "POL-AUTO-0001",
                    HolderName = "Jordan Rivera",
                    HolderEmail = "jordan.rivera@example.com",
                    CoverageType = CoverageType.Auto,
                    CoverageLimit = 20000m,
                    DeductibleAmount = 500m,
                    EffectiveDate = today.AddMonths(-6),
                    ExpiryDate = today.AddMonths(6),
                    Status = PolicyStatus.Active
                },
                new Policy
                {
                    PolicyNumber = "POL-HOME-0001",
                    HolderName = "Casey Morgan",
                    HolderEmail = "casey.morgan@example.com",
                    CoverageType = CoverageType.Home,
                    CoverageLimit = 50000m,
                    DeductibleAmount = 1000m,
                    EffectiveDate = today.AddMonths(-3),
                    ExpiryDate = today.AddMonths(9),
                    Status = PolicyStatus.Active
                });
        }

        await db.SaveChangesAsync();
    }
}
