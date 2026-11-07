using System.Net.Http.Headers;

namespace ClaimFlow.Web.Services;

/// <summary>
/// Reads the JWT stashed in the signed-in user's cookie claims and forwards it as a
/// Bearer token on every outgoing call to the ClaimFlow API.
/// </summary>
public class JwtForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string TokenClaimType = "claimflow:jwt";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirst(TokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
