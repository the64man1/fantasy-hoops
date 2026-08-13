using System.Text.Json.Serialization;

namespace FantasyHoops.Infrastructure.Yahoo;

/// <summary>
/// Persisted Yahoo OAuth2 credentials. The refresh token is the long-lived artifact;
/// the access token is short-lived and re-derived from it on demand.
/// </summary>
public sealed record YahooTokens(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Treated as expired slightly early so a request never starts with a token
    /// that dies mid-flight.
    /// </summary>
    public bool IsExpired(TimeSpan skew) => DateTimeOffset.UtcNow >= ExpiresAt - skew;
}
