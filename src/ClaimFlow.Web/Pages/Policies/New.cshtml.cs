using System.ComponentModel.DataAnnotations;
using ClaimFlow.Web.Contracts.Policies;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages.Policies;

[Authorize(Roles = "Admin")]
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

        var request = new CreatePolicyRequest(
            Input.PolicyNumber,
            Input.HolderName,
            Input.HolderEmail,
            Input.CoverageType,
            Input.CoverageLimit,
            Input.DeductibleAmount,
            Input.EffectiveDate,
            Input.ExpiryDate);

        var result = await apiClient.CreatePolicyAsync(request);
        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Could not create the policy.";
            return Page();
        }

        return RedirectToPage("/Policies/Index");
    }

    public class InputModel
    {
        [Required, StringLength(32), Display(Name = "Policy number")]
        public string PolicyNumber { get; set; } = "";

        [Required, StringLength(128), Display(Name = "Holder name")]
        public string HolderName { get; set; } = "";

        [Required, EmailAddress, Display(Name = "Holder email")]
        public string HolderEmail { get; set; } = "";

        [Display(Name = "Coverage type")]
        public CoverageType CoverageType { get; set; }

        [Range(0.01, double.MaxValue), Display(Name = "Coverage limit")]
        public decimal CoverageLimit { get; set; }

        [Range(0, double.MaxValue), Display(Name = "Deductible amount")]
        public decimal DeductibleAmount { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Effective date")]
        public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required, DataType(DataType.Date), Display(Name = "Expiry date")]
        public DateOnly ExpiryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
    }
}
