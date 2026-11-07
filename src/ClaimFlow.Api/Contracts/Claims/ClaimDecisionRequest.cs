using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record ClaimDecisionRequest([StringLength(2000)] string? Notes);
