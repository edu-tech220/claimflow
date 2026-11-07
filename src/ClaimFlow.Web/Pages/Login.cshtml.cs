using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ClaimFlow.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimFlow.Web.Pages;

[AllowAnonymous]
public class LoginModel(ClaimFlowApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

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

        var result = await apiClient.LoginAsync(Input.Email, Input.Password);
        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Data.DisplayName),
            new(ClaimTypes.Email, Input.Email),
            new(ClaimTypes.Role, result.Data.Role),
            new(JwtForwardingHandler.TokenClaimType, result.Data.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = result.Data.ExpiresAtUtc });

        return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "/Dashboard" : ReturnUrl);
    }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, Display(Name = "Password")]
        public string Password { get; set; } = "";
    }
}
