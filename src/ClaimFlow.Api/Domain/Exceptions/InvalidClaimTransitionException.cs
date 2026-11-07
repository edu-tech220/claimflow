using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Domain.Exceptions;

public class InvalidClaimTransitionException(ClaimStatus currentStatus, ClaimStatus attemptedStatus)
    : DomainException($"Claim cannot move from '{currentStatus}' to '{attemptedStatus}'.")
{
    public ClaimStatus CurrentStatus { get; } = currentStatus;
    public ClaimStatus AttemptedStatus { get; } = attemptedStatus;
}
