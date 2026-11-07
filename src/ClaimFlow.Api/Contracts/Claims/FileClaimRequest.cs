using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record FileClaimRequest(
    [property: Required, StringLength(32)] string PolicyNumber,
    DateOnly IncidentDate,
    [property: Required, StringLength(2000, MinimumLength = 10)] string Description,
    [property: Range(0.01, double.MaxValue)] decimal ClaimedAmount);
