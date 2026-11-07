using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
