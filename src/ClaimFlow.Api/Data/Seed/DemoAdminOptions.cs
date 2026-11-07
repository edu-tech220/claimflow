namespace ClaimFlow.Api.Data.Seed;

public class DemoAdminOptions
{
    public const string SectionName = "DemoAdmin";

    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}
