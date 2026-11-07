using System.ComponentModel.DataAnnotations;

namespace ClaimFlow.Api.Contracts.Claims;

public record SupervisorDecisionRequest(bool Approve, [StringLength(2000)] string? Notes);
