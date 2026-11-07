using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Contracts.Policies;

public record PolicyResponse(
    int Id,
    string PolicyNumber,
    string HolderName,
    string HolderEmail,
    CoverageType CoverageType,
    decimal CoverageLimit,
    decimal DeductibleAmount,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    PolicyStatus Status,
    int Version)
{
    public static PolicyResponse FromEntity(Policy policy) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.HolderName,
        policy.HolderEmail,
        policy.CoverageType,
        policy.CoverageLimit,
        policy.DeductibleAmount,
        policy.EffectiveDate,
        policy.ExpiryDate,
        policy.Status,
        policy.Version);
}
