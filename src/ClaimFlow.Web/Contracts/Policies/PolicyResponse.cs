namespace ClaimFlow.Web.Contracts.Policies;

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
    int Version);
