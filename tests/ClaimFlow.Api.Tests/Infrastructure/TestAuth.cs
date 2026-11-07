using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClaimFlow.Api.Contracts.Auth;

namespace ClaimFlow.Api.Tests.Infrastructure;

public static class TestAuth
{
    public const string AdminEmail = "admin@claimflow.dev";
    public const string AdminPassword = "Admin#12345";
    public const string AdjusterEmail = "adjuster@claimflow.dev";
    public const string AdjusterPassword = "Adjuster#12345";
    public const string SupervisorEmail = "supervisor@claimflow.dev";
    public const string SupervisorPassword = "Supervisor#12345";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.Token;
    }

    public static async Task AuthenticateAsAsync(HttpClient client, string email, string password)
    {
        var token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
