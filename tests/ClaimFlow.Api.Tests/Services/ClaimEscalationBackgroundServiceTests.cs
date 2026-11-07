using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimFlow.Api.Tests.Services;

public class ClaimEscalationBackgroundServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ClaimFlowDbContext _db;

    public ClaimEscalationBackgroundServiceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ClaimFlowDbContext>().UseSqlite(_connection).Options;
        _db = new ClaimFlowDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Policy BuildPolicy() => new()
    {
        PolicyNumber = "POL-ESCALATION-TEST",
        HolderName = "Escalation Test",
        HolderEmail = "escalation@example.com",
        CoverageType = CoverageType.Auto,
        CoverageLimit = 10000m,
        DeductibleAmount = 500m,
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
        Status = PolicyStatus.Active
    };

    private async Task<Policy> SavedPolicyAsync()
    {
        var policy = BuildPolicy();
        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    private async Task<User> SavedAdjusterAsync()
    {
        var adjuster = new User
        {
            Email = "adjuster@escalation-test.dev",
            DisplayName = "Escalation Test Adjuster",
            PasswordHash = "n/a",
            PasswordSalt = "n/a",
            Role = UserRole.Adjuster,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Users.Add(adjuster);
        await _db.SaveChangesAsync();
        return adjuster;
    }

    [Fact]
    public async Task RunSweepAsync_EscalatesClaimsPastSlaDeadline()
    {
        var nowUtc = DateTime.UtcNow;
        var policy = await SavedPolicyAsync();
        var adjuster = await SavedAdjusterAsync();
        var staleClaim = Claim.File(policy, DateOnly.FromDateTime(nowUtc), "Stale claim awaiting review.", 1000m, nowUtc.AddHours(-100));
        staleClaim.AssignAdjuster(adjuster.Id, nowUtc.AddHours(-100));

        _db.Claims.Add(staleClaim);
        await _db.SaveChangesAsync();

        var rules = new ClaimRulesOptions { ReviewSlaHours = 72 };
        var escalatedCount = await ClaimEscalationBackgroundService.RunSweepAsync(_db, rules, nowUtc, CancellationToken.None);

        Assert.Equal(1, escalatedCount);
        var reloaded = await _db.Claims.AsNoTracking().SingleAsync(c => c.Id == staleClaim.Id);
        Assert.True(reloaded.IsEscalated);
    }

    [Fact]
    public async Task RunSweepAsync_DoesNotEscalateClaimsWithinSla()
    {
        var nowUtc = DateTime.UtcNow;
        var policy = await SavedPolicyAsync();
        var adjuster = await SavedAdjusterAsync();
        var freshClaim = Claim.File(policy, DateOnly.FromDateTime(nowUtc), "Fresh claim.", 1000m, nowUtc.AddHours(-2));
        freshClaim.AssignAdjuster(adjuster.Id, nowUtc.AddHours(-2));

        _db.Claims.Add(freshClaim);
        await _db.SaveChangesAsync();

        var rules = new ClaimRulesOptions { ReviewSlaHours = 72 };
        var escalatedCount = await ClaimEscalationBackgroundService.RunSweepAsync(_db, rules, nowUtc, CancellationToken.None);

        Assert.Equal(0, escalatedCount);
    }

    [Fact]
    public async Task RunSweepAsync_DoesNotEscalateClaimsAlreadyApproved()
    {
        var nowUtc = DateTime.UtcNow;
        var policy = await SavedPolicyAsync();
        var adjuster = await SavedAdjusterAsync();
        var approvedClaim = Claim.File(policy, DateOnly.FromDateTime(nowUtc), "Approved long ago.", 1000m, nowUtc.AddHours(-200));
        approvedClaim.AssignAdjuster(adjuster.Id, nowUtc.AddHours(-200));
        approvedClaim.Approve(500m, false, null, adjuster.Id, nowUtc.AddHours(-199));

        _db.Claims.Add(approvedClaim);
        await _db.SaveChangesAsync();

        var rules = new ClaimRulesOptions { ReviewSlaHours = 72 };
        var escalatedCount = await ClaimEscalationBackgroundService.RunSweepAsync(_db, rules, nowUtc, CancellationToken.None);

        Assert.Equal(0, escalatedCount);
    }
}
