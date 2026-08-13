using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FantasyHoops.Infrastructure.Yahoo;

/// <summary>
/// Owns the Yahoo OAuth2 token lifecycle: initial authorization-code exchange, and
/// unattended refresh thereafter. Callers ask for a valid access token and never
/// reason about expiry themselves.
/// </summary>
public sealed class YahooAuthService(
    HttpClient http,
    IOptions<YahooOptions> options,
    IYahooTokenStore store,
    ILogger<YahooAuthService> logger)
{
    private const string AuthorizeUrl = "https://api.login.yahoo.com/oauth2/request_auth";
    private const string TokenUrl = "https://api.login.yahoo.com/oauth2/get_token";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    private readonly YahooOptions _options = options.Value;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public string BuildAuthorizationUrl(string? state = null)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["language"] = "en-us",
        };
        if (!string.IsNullOrEmpty(state)) query["state"] = state;

        var qs = string.Join("&", query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        return $"{AuthorizeUrl}?{qs}";
    }

    public async Task<YahooTokens> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var tokens = await PostTokenRequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        }, existingRefreshToken: null, ct);

        await store.SaveAsync(tokens, ct);
        logger.LogInformation("Yahoo authorization code exchanged; token expires at {ExpiresAt:o}", tokens.ExpiresAt);
        return tokens;
    }

    /// <summary>
    /// Returns a usable access token, refreshing first if the stored one is at or near expiry.
    /// Throws if no tokens have been stored yet — the authorization flow must run once by hand.
    /// </summary>
    public async Task<string> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        var tokens = await store.LoadAsync(ct)
            ?? throw new InvalidOperationException(
                "No Yahoo tokens stored. Complete the authorization flow at /yahoo/auth/start first.");

        if (!tokens.IsExpired(RefreshSkew)) return tokens.AccessToken;

        await _refreshGate.WaitAsync(ct);
        try
        {
            // Re-read inside the gate: a concurrent caller may already have refreshed.
            tokens = await store.LoadAsync(ct) ?? tokens;
            if (!tokens.IsExpired(RefreshSkew)) return tokens.AccessToken;

            logger.LogInformation("Yahoo access token expired; refreshing");
            var refreshed = await PostTokenRequestAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokens.RefreshToken,
                ["redirect_uri"] = _options.RedirectUri,
            }, existingRefreshToken: tokens.RefreshToken, ct);

            await store.SaveAsync(refreshed, ct);
            return refreshed.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    /// <summary>
    /// Refreshes unconditionally, ignoring the recorded expiry. Used when Yahoo rejects a token
    /// that we still believed was valid.
    /// </summary>
    public async Task<string> ForceRefreshAsync(CancellationToken ct = default)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var tokens = await store.LoadAsync(ct)
                ?? throw new InvalidOperationException(
                    "No Yahoo tokens stored. Complete the authorization flow at /yahoo/auth/start first.");

            var refreshed = await PostTokenRequestAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokens.RefreshToken,
                ["redirect_uri"] = _options.RedirectUri,
            }, existingRefreshToken: tokens.RefreshToken, ct);

            await store.SaveAsync(refreshed, ct);
            return refreshed.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<YahooTokens> PostTokenRequestAsync(
        Dictionary<string, string> form,
        string? existingRefreshToken,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form),
        };

        // Yahoo accepts client credentials via HTTP Basic on the token endpoint.
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new YahooAuthException(
                $"Yahoo token request failed ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new YahooAuthException("Yahoo token response contained no access_token.");

        // Yahoo returns a fresh refresh_token on most responses, but fall back to the
        // existing one rather than losing the ability to refresh.
        var refreshToken = root.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString() ?? existingRefreshToken
            : existingRefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            throw new YahooAuthException("Yahoo token response contained no refresh_token.");

        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        logger.LogInformation("Yahoo token acquired; expires_in={ExpiresIn}s", expiresIn);

        return new YahooTokens(
            accessToken,
            refreshToken,
            DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }
}

public sealed class YahooAuthException(string message) : Exception(message);
