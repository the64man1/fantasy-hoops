using System.Text.Json;

namespace FantasyHoops.Infrastructure.Yahoo;

/// <summary>
/// Spike-grade token persistence. Deliberately stores outside the repository tree so a
/// refresh token can never be committed. Moves to the database alongside the ingestion
/// pipeline; the interface is what the rest of the code depends on.
/// </summary>
public sealed class FileYahooTokenStore : IYahooTokenStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileYahooTokenStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FantasyHoops");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "yahoo-tokens.json");
    }

    public string Location => _path;

    public async Task<YahooTokens?> LoadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path)) return null;
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<YahooTokens>(stream, cancellationToken: ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(YahooTokens tokens, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await using var stream = File.Create(_path);
            await JsonSerializer.SerializeAsync(stream, tokens, cancellationToken: ct);
        }
        finally
        {
            _gate.Release();
        }
    }
}
