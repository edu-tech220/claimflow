using ClaimFlow.Web.Contracts.Claims;
using ClaimFlow.Web.Contracts.Common;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages;

[Authorize]
public class DashboardModel(ClaimFlowApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResponse<ClaimResponse>? Claims { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await apiClient.GetClaimsAsync(Status, policyId: null, page: PageNumber, pageSize: 10);
        if (result.IsSuccess)
        {
            Claims = result.Data;
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }
    }

    public string BadgeClass(ClaimStatus status) => status switch
    {
        ClaimStatus.Submitted => "bg-secondary",
        ClaimStatus.UnderReview => "bg-info text-dark",
        ClaimStatus.PendingSupervisorApproval => "bg-warning text-dark",
        ClaimStatus.Approved => "bg-primary",
        ClaimStatus.Rejected => "bg-danger",
        ClaimStatus.Paid => "bg-success",
        ClaimStatus.Closed => "bg-dark",
        _ => "bg-secondary"
    };
}
