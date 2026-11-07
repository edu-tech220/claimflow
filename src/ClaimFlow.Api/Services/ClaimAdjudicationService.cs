using ClaimFlow.Api.Domain.Entities;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Services;

/// <summary>
/// Pure business calculation used when an adjuster approves a claim: applies the
/// policy deductible, caps the payout at whatever coverage limit remains for the
/// policy, and decides whether the amount is large enough to need supervisor sign-off.
/// </summary>
public class ClaimAdjudicationService(IOptions<ClaimRulesOptions> options)
{
    private readonly ClaimRulesOptions _rules = options.Value;

    public AdjudicationResult Adjudicate(Policy policy, Claim claim, decimal alreadyPaidForPolicy)
    {
        if (claim.ClaimedAmount <= policy.DeductibleAmount)
        {
            return AdjudicationResult.Rejected(
                $"Claimed amount ({claim.ClaimedAmount:C}) does not exceed the policy deductible ({policy.DeductibleAmount:C}).");
        }

        var remainingCoverage = policy.CoverageLimit - alreadyPaidForPolicy;
        if (remainingCoverage <= 0)
        {
            return AdjudicationResult.Rejected(
                $"Policy '{policy.PolicyNumber}' has already exhausted its coverage limit of {policy.CoverageLimit:C}.");
        }

        var afterDeductible = claim.ClaimedAmount - policy.DeductibleAmount;
        var payableAmount = Math.Min(afterDeductible, remainingCoverage);
        var requiresSupervisorApproval = payableAmount > _rules.SupervisorApprovalThreshold;

        return AdjudicationResult.Approved(payableAmount, requiresSupervisorApproval);
    }
}
