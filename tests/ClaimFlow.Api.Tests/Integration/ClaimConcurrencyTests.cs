using System.Net;
using System.Net.Http.Json;
using ClaimFlow.Api.Contracts.Claims;
using ClaimFlow.Api.Tests.Infrastructure;
using Xunit;

namespace ClaimFlow.Api.Tests.Integration;

public class ClaimConcurrencyTests(ClaimFlowWebApplicationFactory factory) : IClassFixture<ClaimFlowWebApplicationFactory>
{
    [Fact]
    public async Task Assign_WithTwoConcurrentRequests_OnlyOneSucceeds()
    {
        var filerClient = factory.CreateClient();
        await TestAuth.AuthenticateAsAsync(filerClient, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword);

        var fileRequest = new FileClaimRequest("POL-AUTO-0001", DateOnly.FromDateTime(DateTime.UtcNow), "Concurrency test incident.", 1500m);
        var fileResponse = await filerClient.PostAsJsonAsync("api/claims", fileRequest);
        var claim = await fileResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();
        await Task.WhenAll(
            TestAuth.AuthenticateAsAsync(clientA, TestAuth.AdjusterEmail, TestAuth.AdjusterPassword),
            TestAuth.AuthenticateAsAsync(clientB, TestAuth.AdminEmail, TestAuth.AdminPassword));

        var assignTaskA = clientA.PostAsync($"api/claims/{claim!.Id}/assign", null);
        var assignTaskB = clientB.PostAsync($"api/claims/{claim.Id}/assign", null);
        var results = await Task.WhenAll(assignTaskA, assignTaskB);

        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);
    }
}
