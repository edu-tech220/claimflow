using ClaimFlow.Web.Contracts.Common;
using ClaimFlow.Web.Contracts.Policies;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages.Policies;

[Authorize]
public class IndexModel(ClaimFlowApiClient apiClient) : PageModel
{
    public PagedResponse<PolicyResponse>? Policies { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await apiClient.GetPoliciesAsync();
        if (result.IsSuccess)
        {
            Policies = result.Data;
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }
    }
}
