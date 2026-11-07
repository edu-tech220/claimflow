using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Domain.Exceptions;
using Xunit;

namespace ClaimFlow.Api.Tests.Domain;

public class ClaimStateMachineTests
{
    private static readonly DateTime NowUtc = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static Policy ActivePolicy(decimal coverageLimit = 20000m, decimal deductible = 500m) => new()
    {
        Id = 1,
        PolicyNumber = "POL-TEST-0001",
        HolderName = "Test Holder",
        HolderEmail = "holder@example.com",
        CoverageType = CoverageType.Auto,
        CoverageLimit = coverageLimit,
        DeductibleAmount = deductible,
        EffectiveDate = DateOnly.FromDateTime(NowUtc).AddMonths(-6),
        ExpiryDate = DateOnly.FromDateTime(NowUtc).AddMonths(6),
        Status = PolicyStatus.Active
    };

    [Fact]
    public void File_WithActivePolicyAndValidIncidentDate_CreatesSubmittedClaim()
    {
        var policy = ActivePolicy();
        var incidentDate = DateOnly.FromDateTime(NowUtc).AddDays(-1);

        var claim = Claim.File(policy, incidentDate, "Rear-end collision on Main St.", 2000m, NowUtc);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(policy.Id, claim.PolicyId);
        Assert.Single(claim.History);
    }

    [Fact]
    public void File_WithInactivePolicy_ThrowsPolicyNotActiveException()
    {
        var policy = ActivePolicy();
        policy.Status = PolicyStatus.Cancelled;

        Assert.Throws<PolicyNotActiveException>(() =>
            Claim.File(policy, DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc));
    }

    [Fact]
    public void File_WithIncidentDateBeforeCoveragePeriod_ThrowsIncidentOutsideCoverageException()
    {
        var policy = ActivePolicy();
        var tooEarly = policy.EffectiveDate.AddDays(-1);

        Assert.Throws<IncidentOutsideCoverageException>(() =>
            Claim.File(policy, tooEarly, "desc", 1000m, NowUtc));
    }

    [Fact]
    public void File_WithIncidentDateAfterCoveragePeriod_ThrowsIncidentOutsideCoverageException()
    {
        var policy = ActivePolicy();
        var tooLate = policy.ExpiryDate.AddDays(1);

        Assert.Throws<IncidentOutsideCoverageException>(() =>
            Claim.File(policy, tooLate, "desc", 1000m, NowUtc));
    }

    [Fact]
    public void AssignAdjuster_FromSubmitted_MovesToUnderReview()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);

        claim.AssignAdjuster(adjusterUserId: 42, NowUtc);

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(42, claim.AssignedAdjusterUserId);
        Assert.Equal(2, claim.History.Count);
    }

    [Fact]
    public void AssignAdjuster_WhenNotSubmitted_ThrowsInvalidClaimTransitionException()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);

        var ex = Assert.Throws<InvalidClaimTransitionException>(() => claim.AssignAdjuster(2, NowUtc));
        Assert.Equal(ClaimStatus.UnderReview, ex.CurrentStatus);
        Assert.Equal(ClaimStatus.Submitted, ex.AttemptedStatus);
    }

    [Fact]
    public void Approve_WithoutSupervisorRequirement_MovesStraightToApproved()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);

        claim.Approve(payableAmount: 500m, requiresSupervisorApproval: false, notes: "ok", decidedByUserId: 1, NowUtc);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(500m, claim.PayableAmount);
    }

    [Fact]
    public void Approve_WhenRequiresSupervisor_MovesToPendingSupervisorApproval()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 10000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);

        claim.Approve(payableAmount: 9500m, requiresSupervisorApproval: true, notes: null, decidedByUserId: 1, NowUtc);

        Assert.Equal(ClaimStatus.PendingSupervisorApproval, claim.Status);
    }

    [Fact]
    public void Approve_WhenNotUnderReview_ThrowsInvalidClaimTransitionException()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);

        Assert.Throws<InvalidClaimTransitionException>(() =>
            claim.Approve(500m, false, null, 1, NowUtc));
    }

    [Fact]
    public void SupervisorApprove_FromPending_MovesToApproved()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 10000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);
        claim.Approve(9500m, true, null, 1, NowUtc);

        claim.SupervisorApprove("looks fine", supervisorUserId: 2, NowUtc);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void Reject_FromUnderReview_SetsPayableAmountToZero()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 100m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);

        claim.Reject("deductible not met", decidedByUserId: 1, NowUtc);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal(0m, claim.PayableAmount);
    }

    [Fact]
    public void Reject_FromSubmitted_ThrowsInvalidClaimTransitionException()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 100m, NowUtc);

        Assert.Throws<InvalidClaimTransitionException>(() => claim.Reject("no", 1, NowUtc));
    }

    [Fact]
    public void MarkPaid_FromApproved_MovesToPaid()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);
        claim.Approve(500m, false, null, 1, NowUtc);

        claim.MarkPaid(paidByUserId: 3, NowUtc);

        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }

    [Fact]
    public void MarkPaid_WhenNotApproved_ThrowsInvalidClaimTransitionException()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);

        Assert.Throws<InvalidClaimTransitionException>(() => claim.MarkPaid(3, NowUtc));
    }

    [Fact]
    public void Close_FromPaidOrRejected_Succeeds()
    {
        var paidClaim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);
        paidClaim.AssignAdjuster(1, NowUtc);
        paidClaim.Approve(500m, false, null, 1, NowUtc);
        paidClaim.MarkPaid(3, NowUtc);
        paidClaim.Close(3, NowUtc);

        Assert.Equal(ClaimStatus.Closed, paidClaim.Status);
    }

    [Fact]
    public void Close_FromUnderReview_ThrowsInvalidClaimTransitionException()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);
        claim.AssignAdjuster(1, NowUtc);

        Assert.Throws<InvalidClaimTransitionException>(() => claim.Close(1, NowUtc));
    }

    [Fact]
    public void Escalate_IsIdempotent()
    {
        var claim = Claim.File(ActivePolicy(), DateOnly.FromDateTime(NowUtc), "desc", 1000m, NowUtc);

        claim.Escalate(NowUtc);
        var firstEscalatedAt = claim.EscalatedAtUtc;
        claim.Escalate(NowUtc.AddHours(1));

        Assert.True(claim.IsEscalated);
        Assert.Equal(firstEscalatedAt, claim.EscalatedAtUtc);
    }
}
