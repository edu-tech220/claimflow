namespace ClaimFlow.Api.Domain.Enums;

public enum ClaimStatus
{
    Submitted,
    UnderReview,
    PendingSupervisorApproval,
    Approved,
    Rejected,
    Paid,
    Closed
}
