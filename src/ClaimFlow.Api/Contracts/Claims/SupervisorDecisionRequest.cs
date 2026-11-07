using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record SupervisorDecisionRequest(bool Approve, [property: StringLength(2000)] string? Notes);
