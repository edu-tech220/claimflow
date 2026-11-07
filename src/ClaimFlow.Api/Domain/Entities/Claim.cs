using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Domain.Exceptions;

namespace ClaimFlow.Api.Domain.Entities;

public class Claim : IHasConcurrencyVersion
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

    public int Version { get; set; }

    public static Claim File(Policy policy, DateOnly incidentDate, string description, decimal claimedAmount, DateTime nowUtc)
    {
        if (policy.Status != PolicyStatus.Active)
        {
            throw new PolicyNotActiveException(policy.PolicyNumber);
        }

        if (incidentDate < policy.EffectiveDate || incidentDate > policy.ExpiryDate)
        {
            throw new IncidentOutsideCoverageException(policy.PolicyNumber, incidentDate);
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(claimedAmount, 0m, nameof(claimedAmount));

        var claim = new Claim
        {
            PolicyId = policy.Id,
            Policy = policy,
            IncidentDate = incidentDate,
            FiledAtUtc = nowUtc,
            Description = description,
            ClaimedAmount = claimedAmount,
            Status = ClaimStatus.Submitted
        };

        claim.History.Add(new ClaimStatusHistory
        {
            FromStatus = ClaimStatus.Submitted,
            ToStatus = ClaimStatus.Submitted,
            ChangedAtUtc = nowUtc,
            Notes = "Claim filed by policy holder."
        });

        return claim;
    }

    public void AssignAdjuster(int adjusterUserId, DateTime nowUtc)
    {
        EnsureStatus(ClaimStatus.Submitted);

        Status = ClaimStatus.UnderReview;
        AssignedAdjusterUserId = adjusterUserId;
        RecordTransition(ClaimStatus.Submitted, ClaimStatus.UnderReview, nowUtc, adjusterUserId, "Assigned to adjuster for review.");
    }

    public void Approve(decimal payableAmount, bool requiresSupervisorApproval, string? notes, int decidedByUserId, DateTime nowUtc)
    {
        EnsureStatus(ClaimStatus.UnderReview);

        var fromStatus = Status;
        var toStatus = requiresSupervisorApproval ? ClaimStatus.PendingSupervisorApproval : ClaimStatus.Approved;

        PayableAmount = payableAmount;
        DecisionNotes = notes;
        DecidedAtUtc = nowUtc;
        Status = toStatus;

        RecordTransition(fromStatus, toStatus, nowUtc, decidedByUserId, notes);
    }

    public void SupervisorApprove(string? notes, int supervisorUserId, DateTime nowUtc)
    {
        EnsureStatus(ClaimStatus.PendingSupervisorApproval);

        Status = ClaimStatus.Approved;
        DecisionNotes = notes ?? DecisionNotes;
        DecidedAtUtc = nowUtc;

        RecordTransition(ClaimStatus.PendingSupervisorApproval, ClaimStatus.Approved, nowUtc, supervisorUserId, notes);
    }

    public void Reject(string notes, int decidedByUserId, DateTime nowUtc)
    {
        if (Status is not (ClaimStatus.UnderReview or ClaimStatus.PendingSupervisorApproval))
        {
            throw new InvalidClaimTransitionException(Status, ClaimStatus.Rejected);
        }

        var fromStatus = Status;
        Status = ClaimStatus.Rejected;
        PayableAmount = 0m;
        DecisionNotes = notes;
        DecidedAtUtc = nowUtc;

        RecordTransition(fromStatus, ClaimStatus.Rejected, nowUtc, decidedByUserId, notes);
    }

    public void MarkPaid(int paidByUserId, DateTime nowUtc)
    {
        EnsureStatus(ClaimStatus.Approved);

        Status = ClaimStatus.Paid;
        RecordTransition(ClaimStatus.Approved, ClaimStatus.Paid, nowUtc, paidByUserId, "Payment issued to policy holder.");
    }

    public void Close(int closedByUserId, DateTime nowUtc)
    {
        if (Status is not (ClaimStatus.Paid or ClaimStatus.Rejected))
        {
            throw new InvalidClaimTransitionException(Status, ClaimStatus.Closed);
        }

        var fromStatus = Status;
        Status = ClaimStatus.Closed;

        RecordTransition(fromStatus, ClaimStatus.Closed, nowUtc, closedByUserId, "Claim closed.");
    }

    public void Escalate(DateTime nowUtc)
    {
        if (IsEscalated)
        {
            return;
        }

        IsEscalated = true;
        EscalatedAtUtc = nowUtc;
    }

    private void EnsureStatus(ClaimStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidClaimTransitionException(Status, expected);
        }
    }

    private void RecordTransition(ClaimStatus fromStatus, ClaimStatus toStatus, DateTime nowUtc, int? changedByUserId, string? notes)
    {
        History.Add(new ClaimStatusHistory
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedAtUtc = nowUtc,
            ChangedByUserId = changedByUserId,
            Notes = notes
        });
    }
}
