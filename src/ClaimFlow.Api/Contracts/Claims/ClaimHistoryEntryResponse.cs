using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Contracts.Claims;

public record ClaimHistoryEntryResponse(
    ClaimStatus FromStatus,
    ClaimStatus ToStatus,
    string? Notes,
    int? ChangedByUserId,
    DateTime ChangedAtUtc)
{
    public static ClaimHistoryEntryResponse FromEntity(ClaimStatusHistory history) => new(
        history.FromStatus,
        history.ToStatus,
        history.Notes,
        history.ChangedByUserId,
        history.ChangedAtUtc);
}
