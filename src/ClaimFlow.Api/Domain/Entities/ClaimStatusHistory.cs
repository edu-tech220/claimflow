using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Domain.Entities;

public class ClaimStatusHistory
{
    public int Id { get; set; }

    public int ClaimId { get; set; }
    public Claim Claim { get; set; } = default!;

    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
