using System.ComponentModel.DataAnnotations;
using ClaimFlow.Web.Contracts.Claims;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages.Claims;

[Authorize]
public class NewModel(ClaimFlowApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new FileClaimRequest(Input.PolicyNumber, Input.IncidentDate, Input.Description, Input.ClaimedAmount);
        var result = await apiClient.FileClaimAsync(request);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Could not file the claim.";
            return Page();
        }

        return RedirectToPage("/Claims/Details", new { id = result.Data.Id });
    }

    public class InputModel
    {
        [Required, StringLength(32), Display(Name = "Policy number")]
        public string PolicyNumber { get; set; } = "";

        [Required, DataType(DataType.Date), Display(Name = "Incident date")]
        public DateOnly IncidentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required, StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = "";

        [Range(0.01, double.MaxValue), Display(Name = "Claimed amount")]
        public decimal ClaimedAmount { get; set; }
    }
}
