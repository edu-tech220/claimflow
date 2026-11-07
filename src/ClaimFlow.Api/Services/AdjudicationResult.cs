namespace ClaimFlow.Api.Services;

public record AdjudicationResult
{
    public required bool IsApproved { get; init; }
    public decimal PayableAmount { get; init; }
    public bool RequiresSupervisorApproval { get; init; }
    public string? RejectionReason { get; init; }

    public static AdjudicationResult Approved(decimal payableAmount, bool requiresSupervisorApproval) => new()
    {
        IsApproved = true,
        PayableAmount = payableAmount,
        RequiresSupervisorApproval = requiresSupervisorApproval
    };

    public static AdjudicationResult Rejected(string reason) => new()
    {
        IsApproved = false,
        RejectionReason = reason
    };
}
