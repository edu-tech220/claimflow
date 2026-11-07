using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Contracts.Claims;

public record ClaimResponse(
    int Id,
    string ClaimNumber,
    int PolicyId,
    string PolicyNumber,
    DateOnly IncidentDate,
    DateTime FiledAtUtc,
    string Description,
    decimal ClaimedAmount,
    decimal? PayableAmount,
    ClaimStatus Status,
    int? AssignedAdjusterUserId,
    string? AssignedAdjusterName,
    string? DecisionNotes,
    DateTime? DecidedAtUtc,
    bool IsEscalated,
    int Version)
{
    public static ClaimResponse FromEntity(Claim claim) => new(
        claim.Id,
        claim.ClaimNumber,
        claim.PolicyId,
        claim.Policy.PolicyNumber,
        claim.IncidentDate,
        claim.FiledAtUtc,
        claim.Description,
        claim.ClaimedAmount,
        claim.PayableAmount,
        claim.Status,
        claim.AssignedAdjusterUserId,
        claim.AssignedAdjuster?.DisplayName,
        claim.DecisionNotes,
        claim.DecidedAtUtc,
        claim.IsEscalated,
        claim.Version);
}
