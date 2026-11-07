using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record FileClaimRequest(
    [Required, StringLength(32)] string PolicyNumber,
    DateOnly IncidentDate,
    [Required, StringLength(2000, MinimumLength = 10)] string Description,
    [Range(0.01, double.MaxValue)] decimal ClaimedAmount);
