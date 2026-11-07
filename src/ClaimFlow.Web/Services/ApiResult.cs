using System.Net.Http.Json;
using System.Text.Json;

namespace ClaimFlow.Web.Services;

public record ApiResult<T>(bool IsSuccess, T? Data, string? ErrorMessage, int StatusCode)
{
    public static async Task<ApiResult<T>> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return new ApiResult<T>(true, data, null, statusCode);
        }

        var errorMessage = await ExtractErrorMessageAsync(response, cancellationToken);
        return new ApiResult<T>(false, default, errorMessage, statusCode);
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(cancellationToken: cancellationToken);
            if (problem is not null)
            {
                if (problem.Errors is { Count: > 0 })
                {
                    return string.Join(" ", problem.Errors.SelectMany(kv => kv.Value));
                }

                if (!string.IsNullOrWhiteSpace(problem.Detail))
                {
                    return problem.Detail;
                }

                if (!string.IsNullOrWhiteSpace(problem.Title))
                {
                    return problem.Title;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Response body was not a problem-details payload; fall through to the generic message.
        }

        return $"Request failed with status code {(int)response.StatusCode}.";
    }

    private record ProblemDetailsPayload(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
