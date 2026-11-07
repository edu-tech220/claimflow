using ClaimFlow.Api.Contracts.Claims;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Services;

/// <summary>
/// Orchestrates claim state transitions against the database, including the
/// deductible/coverage-limit adjudication math and optimistic-concurrency retries
/// for the operations most likely to race (two adjusters grabbing the same claim).
/// </summary>
public class ClaimsService(ClaimFlowDbContext db, ClaimAdjudicationService adjudicationService)
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<Claim> FileClaimAsync(FileClaimRequest request, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var policy = await db.Policies.FirstOrDefaultAsync(p => p.PolicyNumber == request.PolicyNumber, cancellationToken)
            ?? throw new KeyNotFoundException($"Policy '{request.PolicyNumber}' was not found.");

        var claim = Claim.File(policy, request.IncidentDate, request.Description, request.ClaimedAmount, nowUtc);
        db.Claims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);

        claim.ClaimNumber = $"CLM-{claim.Id:D6}";
        await db.SaveChangesAsync(cancellationToken);

        return claim;
    }

    public Task<Claim> AssignAdjusterAsync(int claimId, int adjusterUserId, DateTime nowUtc, CancellationToken cancellationToken) =>
        UpdateWithRetryAsync(claimId, claim => claim.AssignAdjuster(adjusterUserId, nowUtc), cancellationToken);

    public async Task<Claim> ApproveAsync(int claimId, int decidedByUserId, string? notes, DateTime nowUtc, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var claim = await LoadClaimAsync(claimId, cancellationToken);

            // Summed client-side: SQLite's EF Core provider cannot translate Sum() over
            // decimal columns server-side, and the per-policy claim count is small enough
            // that this stays cheap while remaining portable across both providers.
            var payableAmounts = await db.Claims
                .Where(c => c.PolicyId == claim.PolicyId
                    && c.Id != claim.Id
                    && (c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Paid))
                .Select(c => c.PayableAmount)
                .ToListAsync(cancellationToken);
            var alreadyPaidForPolicy = payableAmounts.Sum(amount => amount ?? 0m);

            var adjudication = adjudicationService.Adjudicate(claim.Policy, claim, alreadyPaidForPolicy);

            if (adjudication.IsApproved)
            {
                claim.Approve(adjudication.PayableAmount, adjudication.RequiresSupervisorApproval, notes, decidedByUserId, nowUtc);
            }
            else
            {
                claim.Reject(adjudication.RejectionReason!, decidedByUserId, nowUtc);
            }

            if (await TrySaveAsync(attempt, cancellationToken))
            {
                return claim;
            }
        }

        throw new InvalidOperationException($"Could not approve claim {claimId} after multiple attempts due to concurrent updates.");
    }

    public Task<Claim> SupervisorDecideAsync(int claimId, bool approve, string? notes, int supervisorUserId, DateTime nowUtc, CancellationToken cancellationToken) =>
        UpdateWithRetryAsync(claimId, claim =>
        {
            if (approve)
            {
                claim.SupervisorApprove(notes, supervisorUserId, nowUtc);
            }
            else
            {
                claim.Reject(notes ?? "Rejected by supervisor.", supervisorUserId, nowUtc);
            }
        }, cancellationToken);

    public Task<Claim> RejectAsync(int claimId, string notes, int decidedByUserId, DateTime nowUtc, CancellationToken cancellationToken) =>
        UpdateWithRetryAsync(claimId, claim => claim.Reject(notes, decidedByUserId, nowUtc), cancellationToken);

    public Task<Claim> MarkPaidAsync(int claimId, int paidByUserId, DateTime nowUtc, CancellationToken cancellationToken) =>
        UpdateWithRetryAsync(claimId, claim => claim.MarkPaid(paidByUserId, nowUtc), cancellationToken);

    private async Task<Claim> UpdateWithRetryAsync(int claimId, Action<Claim> mutate, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var claim = await LoadClaimAsync(claimId, cancellationToken);
            mutate(claim);

            if (await TrySaveAsync(attempt, cancellationToken))
            {
                return claim;
            }
        }

        throw new InvalidOperationException($"Could not update claim {claimId} after multiple attempts due to concurrent updates.");
    }

    private async Task<Claim> LoadClaimAsync(int claimId, CancellationToken cancellationToken) =>
        await db.Claims
            .Include(c => c.Policy)
            .Include(c => c.AssignedAdjuster)
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken)
        ?? throw new KeyNotFoundException($"Claim {claimId} was not found.");

    private async Task<bool> TrySaveAsync(int attempt, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
