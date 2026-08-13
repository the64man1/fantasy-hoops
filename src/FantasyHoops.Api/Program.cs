using FantasyHoops.Infrastructure;
using FantasyHoops.Infrastructure.Yahoo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddYahooIntegration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem("Database unreachable", statusCode: 503);
})
.WithName("Health");

// ---------------------------------------------------------------------------
// Yahoo OAuth spike endpoints.
//
// These exist to answer the week-1 go/no-go: can we authorize once by hand, then
// refresh unattended, and pull real box scores? They are scaffolding for that
// question, not the shape of the eventual ingestion pipeline.
// ---------------------------------------------------------------------------

var yahoo = app.MapGroup("/yahoo");

yahoo.MapGet("/auth/start", (YahooAuthService auth, IOptions<YahooOptions> opts) =>
{
    if (!opts.Value.IsConfigured)
    {
        return Results.Problem(
            "Yahoo ClientId/ClientSecret are not configured. Set them via dotnet user-secrets.",
            statusCode: 500);
    }
    return Results.Redirect(auth.BuildAuthorizationUrl());
});

yahoo.MapGet("/auth/callback", async (string? code, string? error, YahooAuthService auth, CancellationToken ct) =>
{
    if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });
    if (string.IsNullOrEmpty(code)) return Results.BadRequest(new { error = "No authorization code returned." });

    var tokens = await auth.ExchangeCodeAsync(code, ct);
    return Results.Ok(new
    {
        status = "authorized",
        expiresAt = tokens.ExpiresAt,
        // Surfaced explicitly: the real token lifetime was an open question in the scope doc.
        accessTokenLifetimeSeconds = (int)(tokens.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds,
    });
});

yahoo.MapGet("/auth/status", async (IYahooTokenStore store, FileYahooTokenStore fileStore, CancellationToken ct) =>
{
    var tokens = await store.LoadAsync(ct);
    return tokens is null
        ? Results.Ok(new { authorized = false, tokenStore = fileStore.Location })
        : Results.Ok(new
        {
            authorized = true,
            tokenStore = fileStore.Location,
            expiresAt = tokens.ExpiresAt,
            expired = tokens.IsExpired(TimeSpan.Zero),
        });
});

// Confirms the token works and reveals which season "nba" currently resolves to.
yahoo.MapGet("/spike/game", async (YahooApiClient api, CancellationToken ct) =>
    Results.Content(await api.GetRawAsync("game/nba", ct), "application/json"));

// Yahoo paginates players 25 at a time; a small page is enough to harvest player keys.
yahoo.MapGet("/spike/players", async (YahooApiClient api, int start, int count, CancellationToken ct) =>
    Results.Content(await api.GetRawAsync($"game/nba/players;start={start};count={count}", ct), "application/json"));

// The actual proof: real box-score lines for a real date.
// Defaults to an in-season date because August is the NBA offseason — nothing played last night.
yahoo.MapGet("/spike/stats", async (YahooApiClient api, string playerKeys, string? date, CancellationToken ct) =>
{
    var statDate = date ?? "2026-03-15";
    var path = $"players;player_keys={playerKeys}/stats;type=date;date={statDate}";
    return Results.Content(await api.GetRawAsync(path, ct), "application/json");
});

app.Run();
