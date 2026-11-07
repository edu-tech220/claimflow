using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record RejectClaimRequest([property: Required, StringLength(2000, MinimumLength = 5)] string Notes);
