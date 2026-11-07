namespace ClaimFlow.Web.Contracts.Policies;

public record CreatePolicyRequest(
    string PolicyNumber,
    string HolderName,
    string HolderEmail,
    CoverageType CoverageType,
    decimal CoverageLimit,
    decimal DeductibleAmount,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate);
