using System.Net;
using System.Net.Http.Json;
using ClaimFlow.Api.Contracts.Claims;
using ClaimFlow.Api.Contracts.Policies;
using ClaimFlow.Api.Domain.Enums;
using ClaimFlow.Api.Tests.Infrastructure;
using Xunit;

namespace ClaimFlow.Api.Tests.Integration;

public class ClaimsFlowTests(ClaimFlowWebApplicationFactory factory) : IClassFixture<ClaimFlowWebApplicationFactory>
{
    [Fact]
    public async Task FullLifecycle_FileAssignApproveMarkPaid_Succeeds()
    {
        var anonymousClient = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(anonymousClient, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var filed = await FileClaimAsync(anonymousClient, "POL-AUTO-0001", DateTime.UtcNow, claimedAmount: 2000m);
        Assert.Equal(ClaimStatus.Submitted, filed.Status);

        var assignResponse = await anonymousClient.PostAsync($"api/claims/{filed.Id}/assign", null);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assigned = await assignResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.UnderReview, assigned!.Status);

        var approveResponse = await anonymousClient.PostAsJsonAsync($"api/claims/{filed.Id}/approve", new ClaimDecisionRequest(null));
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = await approveResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.Approved, approved!.Status);
        Assert.Equal(1500m, approved.PayableAmount);

        var adminClient = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(adminClient, TestAuth.AdminEmail, TestAuth.AdminPassword);

        var paidResponse = await adminClient.PostAsync($"api/claims/{filed.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.OK, paidResponse.StatusCode);
        var paid = await paidResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.Paid, paid!.Status);

        var historyResponse = await anonymousClient.GetAsync($"api/claims/{filed.Id}/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<ClaimHistoryEntryResponse>>();
        Assert.True(history!.Count >= 4);
    }

    [Fact]
    public async Task Approve_WhenClaimedAmountDoesNotExceedDeductible_AutoRejects()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var filed = await FileClaimAsync(client, "POL-AUTO-0001", DateTime.UtcNow, claimedAmount: 400m);
        await client.PostAsync($"api/claims/{filed.Id}/assign", null);

        var approveResponse = await client.PostAsJsonAsync($"api/claims/{filed.Id}/approve", new ClaimDecisionRequest(null));

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var result = await approveResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.Rejected, result!.Status);
        Assert.Equal(0m, result.PayableAmount);
    }

    [Fact]
    public async Task Approve_WhenPayableAboveThreshold_RequiresSupervisorApproval()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var filed = await FileClaimAsync(client, "POL-AUTO-0001", DateTime.UtcNow, claimedAmount: 8000m);
        await client.PostAsync($"api/claims/{filed.Id}/assign", null);

        var approveResponse = await client.PostAsJsonAsync($"api/claims/{filed.Id}/approve", new ClaimDecisionRequest(null));
        var approved = await approveResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.PendingSupervisorApproval, approved!.Status);
        Assert.Equal(7500m, approved.PayableAmount);

        var supervisorClient = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(supervisorClient, TestAuth.SupervisorEmail, TestAuth.SupervisorPassword);

        var decisionResponse = await supervisorClient.PostAsJsonAsync(
            $"api/claims/{filed.Id}/supervisor-decision", new SupervisorDecisionRequest(true, "Confirmed."));
        var decided = await decisionResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.Approved, decided!.Status);
    }

    [Fact]
    public async Task Reject_FromUnderReview_SetsPayableAmountToZero()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var filed = await FileClaimAsync(client, "POL-AUTO-0001", DateTime.UtcNow, claimedAmount: 3000m);
        await client.PostAsync($"api/claims/{filed.Id}/assign", null);

        var rejectResponse = await client.PostAsJsonAsync($"api/claims/{filed.Id}/reject", new RejectClaimRequest("Not a covered incident."));

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal(ClaimStatus.Rejected, rejected!.Status);
        Assert.Equal(0m, rejected.PayableAmount);
    }

    [Fact]
    public async Task FileClaim_WithIncidentDateBeforePolicyEffectiveDate_ReturnsConflict()
    {
        var adminClient = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(adminClient, TestAuth.AdminEmail, TestAuth.AdminPassword);

        var policyRequest = new CreatePolicyRequest(
            "POL-COVERAGE-TEST-01", "Coverage Tester", "coverage.tester@example.com",
            CoverageType.Home, 10000m, 500m,
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        await adminClient.PostAsJsonAsync("api/policies", policyRequest);

        var claimRequest = new FileClaimRequest(
            "POL-COVERAGE-TEST-01", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), "Incident before coverage started.", 1000m);
        var response = await adminClient.PostAsJsonAsync("api/claims", claimRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FileClaim_ForUnknownPolicyNumber_ReturnsNotFound()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var request = new FileClaimRequest("POL-DOES-NOT-EXIST", DateOnly.FromDateTime(DateTime.UtcNow), "Some incident.", 1000m);
        var response = await client.PostAsJsonAsync("api/claims", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_WhenAlreadyAssigned_ReturnsConflict()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(client, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var filed = await FileClaimAsync(client, "POL-AUTO-0001", DateTime.UtcNow, claimedAmount: 1200m);
        await client.PostAsync($"api/claims/{filed.Id}/assign", null);

        var secondAssign = await client.PostAsync($"api/claims/{filed.Id}/assign", null);

        Assert.Equal(HttpStatusCode.Conflict, secondAssign.StatusCode);
    }

    private static async Task<ClaimResponse> FileClaimAsync(HttpClient client, string policyNumber, DateTime incidentDateUtc, decimal claimedAmount)
    {
        var request = new FileClaimRequest(policyNumber, DateOnly.FromDateTime(incidentDateUtc), "Integration test incident description.", claimedAmount);
        var response = await client.PostAsJsonAsync("api/claims", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClaimResponse>())!;
    }
}
