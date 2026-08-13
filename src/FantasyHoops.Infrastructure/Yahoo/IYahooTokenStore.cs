namespace FantasyHoops.Infrastructure.Yahoo;

public interface IYahooTokenStore
{
    Task<YahooTokens?> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(YahooTokens tokens, CancellationToken ct = default);
}
