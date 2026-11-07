using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Services;

/// <summary>
/// Periodically flags claims that have sat in UnderReview or PendingSupervisorApproval
/// longer than the configured SLA, so dashboards can surface them for follow-up.
/// </summary>
public class ClaimEscalationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ClaimRulesOptions> options,
    ILogger<ClaimEscalationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.EscalationSweepIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClaimFlowDbContext>();
                var escalatedCount = await RunSweepAsync(db, options.Value, DateTime.UtcNow, stoppingToken);

                if (escalatedCount > 0)
                {
                    logger.LogInformation("Escalated {Count} claim(s) past their review SLA.", escalatedCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Claim escalation sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task<int> RunSweepAsync(ClaimFlowDbContext db, ClaimRulesOptions rules, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var slaDeadline = nowUtc.AddHours(-rules.ReviewSlaHours);

        var candidates = await db.Claims
            .Include(c => c.History)
            .Where(c => !c.IsEscalated && (c.Status == ClaimStatus.UnderReview || c.Status == ClaimStatus.PendingSupervisorApproval))
            .ToListAsync(cancellationToken);

        var escalatedCount = 0;
        foreach (var claim in candidates)
        {
            var lastTransitionAtUtc = claim.History.Count > 0
                ? claim.History.Max(h => h.ChangedAtUtc)
                : claim.FiledAtUtc;

            if (lastTransitionAtUtc <= slaDeadline)
            {
                claim.Escalate(nowUtc);
                escalatedCount++;
            }
        }

        if (escalatedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return escalatedCount;
    }
}
