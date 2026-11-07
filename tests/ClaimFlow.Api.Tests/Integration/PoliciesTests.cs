using System.Net;
using System.Net.Http.Json;
using ClaimFlow.Api.Contracts.Common;
using ClaimFlow.Api.Contracts.Policies;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Tests.Infrastructure;
using Xunit;

namespace ClaimFlow.Api.Tests.Integration;

public class PoliciesTests(ClaimFlowWebApplicationFactory factory) : IClassFixture<ClaimFlowWebApplicationFactory>
{
    [Fact]
    public async Task GetPolicies_AsAuthenticatedUser_ReturnsSeededPolicies()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var response = await client.GetAsync("api/policies?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<PolicyResponse>>();
        Assert.NotNull(paged);
        Assert.Contains(paged!.Items, p => p.PolicyNumber == "POL-AUTO-0001");
    }

    [Fact]
    public async Task CreatePolicy_AsAdmin_Succeeds()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdminEmail, TestAuth.AdminPassword);

        var request = new CreatePolicyRequest(
            "POL-HEALTH-9001", "Riley Chen", "riley.chen@example.com",
            CoverageType.Health, 15000m, 250m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var response = await client.PostAsJsonAsync("api/policies", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PolicyResponse>();
        Assert.Equal("POL-HEALTH-9001", created!.PolicyNumber);
        Assert.Equal(PolicyStatus.Active, created.Status);
    }

    [Fact]
    public async Task CreatePolicy_AsAdjuster_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var request = new CreatePolicyRequest(
            "POL-DENY-0001", "Nobody", "nobody@example.com",
            CoverageType.Auto, 10000m, 500m,
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var response = await client.PostAsJsonAsync("api/policies", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePolicy_WithDuplicateNumber_ReturnsValidationProblem()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdminEmail, TestAuth.AdminPassword);

        var request = new CreatePolicyRequest(
            "POL-AUTO-0001", "Duplicate Holder", "dup@example.com",
            CoverageType.Auto, 10000m, 500m,
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var response = await client.PostAsJsonAsync("api/policies", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
