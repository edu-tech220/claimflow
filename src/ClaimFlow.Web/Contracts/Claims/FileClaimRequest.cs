namespace ClaimFlow.Web.Contracts.Claims;

public record FileClaimRequest(string PolicyNumber, DateOnly IncidentDate, string Description, decimal ClaimedAmount);
