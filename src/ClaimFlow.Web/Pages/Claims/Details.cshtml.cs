using ClaimFlow.Web.Contracts.Claims;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages.Claims;

[Authorize]
public class DetailsModel(ClaimFlowApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public ClaimResponse? Claim { get; set; }
    public IReadOnlyList<ClaimHistoryEntryResponse> History { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Claim is null ? NotFound() : Page();
    }

    public Task<IActionResult> OnPostAssignAsync() =>
        HandleActionAsync(() => apiClient.AssignClaimAsync(Id));

    public Task<IActionResult> OnPostApproveAsync() =>
        HandleActionAsync(() => apiClient.ApproveClaimAsync(Id, Notes));

    public async Task<IActionResult> OnPostRejectAsync()
    {
        if (string.IsNullOrWhiteSpace(Notes))
        {
            ErrorMessage = "Rejecting a claim requires a reason.";
            await LoadAsync();
            return Page();
        }

        return await HandleActionAsync(() => apiClient.RejectClaimAsync(Id, Notes));
    }

    public Task<IActionResult> OnPostSupervisorApproveAsync() =>
        HandleActionAsync(() => apiClient.SupervisorDecisionAsync(Id, approve: true, Notes));

    public async Task<IActionResult> OnPostSupervisorRejectAsync()
    {
        if (string.IsNullOrWhiteSpace(Notes))
        {
            ErrorMessage = "Rejecting a claim requires a reason.";
            await LoadAsync();
            return Page();
        }

        return await HandleActionAsync(() => apiClient.SupervisorDecisionAsync(Id, approve: false, Notes));
    }

    public Task<IActionResult> OnPostMarkPaidAsync() =>
        HandleActionAsync(() => apiClient.MarkClaimPaidAsync(Id));

    private async Task<IActionResult> HandleActionAsync(Func<Task<ApiResult<ClaimResponse>>> action)
    {
        var result = await action();
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            await LoadAsync();
            return Page();
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task LoadAsync()
    {
        var claimResult = await apiClient.GetClaimAsync(Id);
        Claim = claimResult.Data;
        if (!claimResult.IsSuccess)
        {
            ErrorMessage ??= claimResult.ErrorMessage;
        }

        var historyResult = await apiClient.GetClaimHistoryAsync(Id);
        History = historyResult.Data ?? [];
    }
}
