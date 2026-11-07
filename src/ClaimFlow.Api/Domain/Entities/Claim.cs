using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Domain.Entities;

public class Claim
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = default!;

    public int PolicyId { get; set; }
    public Policy Policy { get; set; } = default!;

    public DateOnly IncidentDate { get; set; }
    public DateTime FiledAtUtc { get; set; }
    public string Description { get; set; } = default!;
    public decimal ClaimedAmount { get; set; }
    public decimal? PayableAmount { get; set; }

    public ClaimStatus Status { get; set; }

    public int? AssignedAdjusterUserId { get; set; }
    public User? AssignedAdjuster { get; set; }

    public string? DecisionNotes { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public bool IsEscalated { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }

    public List<ClaimStatusHistory> History { get; set; } = new();

    public byte[] RowVersion { get; set; } = default!;
}
