using System.Net.Http.Json;
using ClaimFlow.Web.Contracts.Auth;
using ClaimFlow.Web.Contracts.Claims;
using ClaimFlow.Web.Contracts.Common;
using ClaimFlow.Web.Contracts.Policies;

namespace ClaimFlow.Web.Services;

public class ClaimFlowApiClient(IHttpClientFactory httpClientFactory)
{
    private const string ClientName = "ClaimFlowApi";

    private HttpClient Client => httpClientFactory.CreateClient(ClientName);

    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), cancellationToken);
        return await ApiResult<LoginResponse>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<PagedResponse<PolicyResponse>>> GetPoliciesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"api/policies?page={page}&pageSize={pageSize}", cancellationToken);
        return await ApiResult<PagedResponse<PolicyResponse>>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<PolicyResponse>> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/policies", request, cancellationToken);
        return await ApiResult<PolicyResponse>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<PagedResponse<ClaimResponse>>> GetClaimsAsync(
        string? status, int? policyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (policyId is not null)
        {
            query.Add($"policyId={policyId}");
        }

        var response = await Client.GetAsync($"api/claims?{string.Join('&', query)}", cancellationToken);
        return await ApiResult<PagedResponse<ClaimResponse>>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<ClaimResponse>> GetClaimAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"api/claims/{id}", cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<ClaimHistoryEntryResponse>>> GetClaimHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"api/claims/{id}/history", cancellationToken);
        return await ApiResult<IReadOnlyList<ClaimHistoryEntryResponse>>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<ClaimResponse>> FileClaimAsync(FileClaimRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/claims", request, cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }

    public Task<ApiResult<ClaimResponse>> AssignClaimAsync(int id, CancellationToken cancellationToken = default) =>
        PostActionAsync(id, "assign", cancellationToken);

    public async Task<ApiResult<ClaimResponse>> ApproveClaimAsync(int id, string? notes, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync($"api/claims/{id}/approve", new ClaimDecisionRequest(notes), cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<ClaimResponse>> RejectClaimAsync(int id, string notes, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync($"api/claims/{id}/reject", new RejectClaimRequest(notes), cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }

    public async Task<ApiResult<ClaimResponse>> SupervisorDecisionAsync(int id, bool approve, string? notes, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync($"api/claims/{id}/supervisor-decision", new SupervisorDecisionRequest(approve, notes), cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }

    public Task<ApiResult<ClaimResponse>> MarkClaimPaidAsync(int id, CancellationToken cancellationToken = default) =>
        PostActionAsync(id, "mark-paid", cancellationToken);

    private async Task<ApiResult<ClaimResponse>> PostActionAsync(int id, string action, CancellationToken cancellationToken)
    {
        var response = await Client.PostAsync($"api/claims/{id}/{action}", null, cancellationToken);
        return await ApiResult<ClaimResponse>.FromResponseAsync(response, cancellationToken);
    }
}
