using System.ComponentModel.DataAnnotations;
using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Contracts.Policies;

public record CreatePolicyRequest(
    [Required, StringLength(32, MinimumLength = 3)] string PolicyNumber,
    [Required, StringLength(128)] string HolderName,
    [Required, EmailAddress] string HolderEmail,
    CoverageType CoverageType,
    [Range(0.01, double.MaxValue)] decimal CoverageLimit,
    [Range(0, double.MaxValue)] decimal DeductibleAmount,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate);
