namespace ClaimFlow.Api.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ClaimFlow";
    public string Audience { get; set; } = "ClaimFlow.Clients";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 60;
}
