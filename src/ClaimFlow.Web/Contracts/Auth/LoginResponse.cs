namespace ClaimFlow.Web.Contracts.Auth;

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string DisplayName, string Role);
