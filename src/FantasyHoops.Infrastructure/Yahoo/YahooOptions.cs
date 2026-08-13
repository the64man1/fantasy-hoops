namespace FantasyHoops.Infrastructure.Yahoo;

public sealed class YahooOptions
{
    public const string SectionName = "Yahoo";

    /// <summary>Client ID (Consumer Key) from developer.yahoo.com. Supplied via user-secrets.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Client Secret from developer.yahoo.com. Supplied via user-secrets, never committed.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>Must exactly match the redirect URI registered with the Yahoo app. Yahoo requires HTTPS.</summary>
    public string RedirectUri { get; set; } = "https://localhost:8000/yahoo/auth/callback";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
