namespace ClaimFlow.Web.Contracts.Claims;

public record ClaimHistoryEntryResponse(
    ClaimStatus FromStatus,
    ClaimStatus ToStatus,
    string? Notes,
    int? ChangedByUserId,
    DateTime ChangedAtUtc);
