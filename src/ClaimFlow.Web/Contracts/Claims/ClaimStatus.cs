namespace ClaimFlow.Web.Contracts.Claims;

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
