using ClaimFlow.Api.Contracts;
using ClaimFlow.Api.Contracts.Common;
using ClaimFlow.Api.Contracts.Policies;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Entities;
using ClaimFlow.Api.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/policies")]
public class PoliciesController(ClaimFlowDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PolicyResponse>>> GetPolicies(
        [FromQuery] PolicyStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = db.Policies.AsNoTracking().OrderBy(p => p.PolicyNumber).AsQueryable();
        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        var paged = await query.ToPagedResponseAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(PolicyResponse.FromEntity).ToList();

        return Ok(new PagedResponse<PolicyResponse>(items, paged.Page, paged.PageSize, paged.TotalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PolicyResponse>> GetPolicy(int id, CancellationToken cancellationToken)
    {
        var policy = await db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return policy is null ? NotFound() : Ok(PolicyResponse.FromEntity(policy));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PolicyResponse>> CreatePolicy(CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        if (request.ExpiryDate <= request.EffectiveDate)
        {
            ModelState.AddModelError(nameof(request.ExpiryDate), "Expiry date must be after the effective date.");
            return ValidationProblem(ModelState);
        }

        var policyNumberTaken = await db.Policies.AnyAsync(p => p.PolicyNumber == request.PolicyNumber, cancellationToken);
        if (policyNumberTaken)
        {
            ModelState.AddModelError(nameof(request.PolicyNumber), "A policy with this number already exists.");
            return ValidationProblem(ModelState);
        }

        var policy = new Policy
        {
            PolicyNumber = request.PolicyNumber,
            HolderName = request.HolderName,
            HolderEmail = request.HolderEmail,
            CoverageType = request.CoverageType,
            CoverageLimit = request.CoverageLimit,
            DeductibleAmount = request.DeductibleAmount,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            Status = PolicyStatus.Active
        };

        db.Policies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPolicy), new { id = policy.Id }, PolicyResponse.FromEntity(policy));
    }
}
