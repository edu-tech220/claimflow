using ClaimFlow.Api.Domain.Enums;

namespace ClaimFlow.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string PasswordSalt { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
