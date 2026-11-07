using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record ClaimDecisionRequest([property: StringLength(2000)] string? Notes);
