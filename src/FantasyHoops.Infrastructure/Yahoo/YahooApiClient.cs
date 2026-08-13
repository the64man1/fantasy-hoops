using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace FantasyHoops.Infrastructure.Yahoo;

/// <summary>
/// Thin authenticated transport over the Yahoo Fantasy Sports API. Deliberately returns raw
/// JSON: translating Yahoo's shapes into domain models is the job of a separate mapping layer,
/// so their representation never leaks past this boundary.
/// </summary>
public sealed class YahooApiClient(
    HttpClient http,
    YahooAuthService auth,
    ILogger<YahooApiClient> logger)
{
    private const string BaseUrl = "https://fantasysports.yahooapis.com/fantasy/v2/";

    /// <summary>
    /// GETs a Yahoo resource path (e.g. "game/nba") and returns the raw JSON body.
    /// Yahoo defaults to XML, so format=json is always appended.
    /// </summary>
    public async Task<string> GetRawAsync(string resourcePath, CancellationToken ct = default)
    {
        var url = BuildUrl(resourcePath);

        var response = await SendAuthenticatedAsync(url, forceRefresh: false, ct);

        // A 401 here means the token died earlier than its stated expiry. Refresh once and retry
        // before surfacing an error, since unattended jobs cannot re-authorize interactively.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Yahoo returned 401 for {Path}; forcing token refresh and retrying", resourcePath);
            response.Dispose();
            response = await SendAuthenticatedAsync(url, forceRefresh: true, ct);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new YahooApiException(
                    $"Yahoo request to '{resourcePath}' failed ({(int)response.StatusCode} {response.StatusCode}): {body}",
                    response.StatusCode);
            }
            return body;
        }
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(string url, bool forceRefresh, CancellationToken ct)
    {
        var token = forceRefresh
            ? await auth.ForceRefreshAsync(ct)
            : await auth.GetValidAccessTokenAsync(ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return await http.SendAsync(request, ct);
    }

    private static string BuildUrl(string resourcePath)
    {
        var trimmed = resourcePath.TrimStart('/');
        var separator = trimmed.Contains('?') ? '&' : '?';
        return $"{BaseUrl}{trimmed}{separator}format=json";
    }
}

public sealed class YahooApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
