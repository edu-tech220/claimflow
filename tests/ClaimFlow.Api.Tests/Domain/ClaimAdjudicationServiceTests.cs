using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace ClaimFlow.Api.Tests.Domain;

public class ClaimAdjudicationServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    private static ClaimAdjudicationService CreateService(decimal supervisorThreshold = 5000m) =>
        new(Options.Create(new ClaimRulesOptions { SupervisorApprovalThreshold = supervisorThreshold }));

    private static Policy BuildPolicy(decimal coverageLimit, decimal deductible) => new()
    {
        Id = 1,
        PolicyNumber = "POL-TEST-0001",
        HolderName = "Test Holder",
        HolderEmail = "holder@example.com",
        CoverageType = CoverageType.Auto,
        CoverageLimit = coverageLimit,
        DeductibleAmount = deductible,
        EffectiveDate = Today.AddMonths(-6),
        ExpiryDate = Today.AddMonths(6),
        Status = PolicyStatus.Active
    };

    private static Claim BuildClaim(Policy policy, decimal claimedAmount) =>
        Claim.File(policy, Today, "Test incident", claimedAmount, Today.ToDateTime(TimeOnly.MinValue));

    [Fact]
    public void Adjudicate_WhenClaimedAmountBelowDeductible_IsRejected()
    {
        var policy = BuildPolicy(coverageLimit: 20000m, deductible: 500m);
        var claim = BuildClaim(policy, claimedAmount: 400m);

        var result = CreateService().Adjudicate(policy, claim, alreadyPaidForPolicy: 0m);

        Assert.False(result.IsApproved);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public void Adjudicate_WhenCoverageAlreadyExhausted_IsRejected()
    {
        var policy = BuildPolicy(coverageLimit: 20000m, deductible: 500m);
        var claim = BuildClaim(policy, claimedAmount: 2000m);

        var result = CreateService().Adjudicate(policy, claim, alreadyPaidForPolicy: 20000m);

        Assert.False(result.IsApproved);
    }

    [Fact]
    public void Adjudicate_WithinLimitsAndBelowThreshold_ApprovesWithoutSupervisor()
    {
        var policy = BuildPolicy(coverageLimit: 20000m, deductible: 500m);
        var claim = BuildClaim(policy, claimedAmount: 2000m);

        var result = CreateService(supervisorThreshold: 5000m).Adjudicate(policy, claim, alreadyPaidForPolicy: 0m);

        Assert.True(result.IsApproved);
        Assert.Equal(1500m, result.PayableAmount);
        Assert.False(result.RequiresSupervisorApproval);
    }

    [Fact]
    public void Adjudicate_AbovePayableThreshold_RequiresSupervisorApproval()
    {
        var policy = BuildPolicy(coverageLimit: 20000m, deductible: 500m);
        var claim = BuildClaim(policy, claimedAmount: 10000m);

        var result = CreateService(supervisorThreshold: 5000m).Adjudicate(policy, claim, alreadyPaidForPolicy: 0m);

        Assert.True(result.IsApproved);
        Assert.Equal(9500m, result.PayableAmount);
        Assert.True(result.RequiresSupervisorApproval);
    }

    [Fact]
    public void Adjudicate_PayableAmountIsCappedAtRemainingCoverage()
    {
        var policy = BuildPolicy(coverageLimit: 10000m, deductible: 500m);
        var claim = BuildClaim(policy, claimedAmount: 9000m);

        var result = CreateService().Adjudicate(policy, claim, alreadyPaidForPolicy: 8000m);

        Assert.True(result.IsApproved);
        Assert.Equal(2000m, result.PayableAmount);
    }
}
