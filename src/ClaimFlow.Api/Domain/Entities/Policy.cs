using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Domain.Entities;

public class Policy
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = default!;
    public string HolderName { get; set; } = default!;
    public string HolderEmail { get; set; } = default!;
    public CoverageType CoverageType { get; set; }
    public decimal CoverageLimit { get; set; }
    public decimal DeductibleAmount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public PolicyStatus Status { get; set; }

    public List<Claim> Claims { get; set; } = new();

    public byte[] RowVersion { get; set; } = default!;
}
