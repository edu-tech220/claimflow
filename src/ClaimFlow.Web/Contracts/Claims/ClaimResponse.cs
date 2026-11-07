namespace ClaimFlow.Web.Contracts.Claims;

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
    int Version);
