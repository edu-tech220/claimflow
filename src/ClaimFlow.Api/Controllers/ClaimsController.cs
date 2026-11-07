using System.Security.Claims;
using ClaimFlow.Api.Contracts;
using ClaimFlow.Api.Contracts.Claims;
using ClaimFlow.Api.Contracts.Common;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/claims")]
public class ClaimsController(ClaimFlowDbContext db, ClaimsService claimsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClaimResponse>>> GetClaims(
        [FromQuery] ClaimStatus? status, [FromQuery] int? policyId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = db.Claims
            .AsNoTracking()
            .Include(c => c.Policy)
            .Include(c => c.AssignedAdjuster)
            .OrderByDescending(c => c.FiledAtUtc)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (policyId is not null)
        {
            query = query.Where(c => c.PolicyId == policyId);
        }

        var paged = await query.ToPagedResponseAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(ClaimResponse.FromEntity).ToList();

        return Ok(new PagedResponse<ClaimResponse>(items, paged.Page, paged.PageSize, paged.TotalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaimResponse>> GetClaim(int id, CancellationToken cancellationToken)
    {
        var claim = await db.Claims
            .AsNoTracking()
            .Include(c => c.Policy)
            .Include(c => c.AssignedAdjuster)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return claim is null ? NotFound() : Ok(ClaimResponse.FromEntity(claim));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<ClaimHistoryEntryResponse>>> GetClaimHistory(int id, CancellationToken cancellationToken)
    {
        var claimExists = await db.Claims.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);
        if (!claimExists)
        {
            return NotFound();
        }

        var history = await db.ClaimStatusHistories
            .AsNoTracking()
            .Where(h => h.ClaimId == id)
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => ClaimHistoryEntryResponse.FromEntity(h))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<ClaimResponse>> FileClaim(FileClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await claimsService.FileClaimAsync(request, DateTime.UtcNow, cancellationToken);
        return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, ClaimResponse.FromEntity(claim));
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Adjuster,Admin")]
    public async Task<ActionResult<ClaimResponse>> AssignToSelf(int id, CancellationToken cancellationToken)
    {
        var claim = await claimsService.AssignAdjusterAsync(id, GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
        return Ok(ClaimResponse.FromEntity(claim));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Adjuster,Admin")]
    public async Task<ActionResult<ClaimResponse>> Approve(int id, ClaimDecisionRequest request, CancellationToken cancellationToken)
    {
        var claim = await claimsService.ApproveAsync(id, GetCurrentUserId(), request.Notes, DateTime.UtcNow, cancellationToken);
        return Ok(ClaimResponse.FromEntity(claim));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Adjuster,Supervisor,Admin")]
    public async Task<ActionResult<ClaimResponse>> Reject(int id, RejectClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await claimsService.RejectAsync(id, request.Notes, GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
        return Ok(ClaimResponse.FromEntity(claim));
    }

    [HttpPost("{id:int}/supervisor-decision")]
    [Authorize(Roles = "Supervisor,Admin")]
    public async Task<ActionResult<ClaimResponse>> SupervisorDecide(int id, SupervisorDecisionRequest request, CancellationToken cancellationToken)
    {
        var claim = await claimsService.SupervisorDecideAsync(id, request.Approve, request.Notes, GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
        return Ok(ClaimResponse.FromEntity(claim));
    }

    [HttpPost("{id:int}/mark-paid")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClaimResponse>> MarkPaid(int id, CancellationToken cancellationToken)
    {
        var claim = await claimsService.MarkPaidAsync(id, GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
        return Ok(ClaimResponse.FromEntity(claim));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user is missing a NameIdentifier claim.");
        return int.Parse(value);
    }
}
