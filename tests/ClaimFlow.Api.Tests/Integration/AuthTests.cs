using System.Net;
using System.Net.Http.Json;
using ClaimFlow.Api.Contracts.Auth;
using ClaimFlow.Api.Tests.Infrastructure;
using Xunit;

namespace ClaimFlow.Api.Tests.Integration;

public class AuthTests(ClaimFlowWebApplicationFactory factory) : IClassFixture<ClaimFlowWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(TestAuth.AdminEmail, TestAuth.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.Equal("Admin", payload.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(TestAuth.AdminEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/policies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
