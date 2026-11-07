using System.ComponentModel.DataAnnotations;
using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Contracts.Policies;

public record CreatePolicyRequest(
    [property: Required, StringLength(32, MinimumLength = 3)] string PolicyNumber,
    [property: Required, StringLength(128)] string HolderName,
    [property: Required, EmailAddress] string HolderEmail,
    CoverageType CoverageType,
    [property: Range(0.01, double.MaxValue)] decimal CoverageLimit,
    [property: Range(0, double.MaxValue)] decimal DeductibleAmount,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate);
