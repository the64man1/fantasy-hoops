namespace FantasyHoops.Domain.Lineups;

/// <summary>
/// One NBA game, as lineup rules need to see it.
/// </summary>
/// <param name="FantasyDate">
/// The fantasy day this game counts toward. Deliberately stored rather than derived from
/// <paramref name="TipOff"/>, because the two disagree: a 10:30pm Eastern tip-off on March 15
/// is already March 16 in UTC. Taking the date component of an instant is the single easiest
/// way to attribute a game to the wrong day.
/// </param>
/// <param name="TipOff">
/// The absolute instant the game starts. An offset-bearing value, so comparisons against "now"
/// are unambiguous no matter what zone the server runs in.
/// </param>
public sealed record ScheduledGame(
    DateOnly FantasyDate,
    DateTimeOffset TipOff,
    string HomeTeam,
    string AwayTeam)
{
    public bool Involves(string nbaTeam) =>
        string.Equals(HomeTeam, nbaTeam, StringComparison.OrdinalIgnoreCase)
        || string.Equals(AwayTeam, nbaTeam, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Derives the fantasy day a tip-off belongs to, in the league's timezone.
    /// </summary>
    /// <remarks>
    /// Use when a data provider supplies only an instant. Converting into the league zone before
    /// taking the date is what keeps late tip-offs on the correct day.
    /// <para>
    /// Takes a <see cref="TimeZoneInfo"/> rather than a fixed offset, but note which error that
    /// actually prevents. Slicing on UTC is a five-hour mistake and misfiles every late West Coast
    /// tip-off. A hardcoded league offset is only ever one hour wrong (Eastern is UTC-5 in January
    /// and UTC-4 in July), and one hour only crosses a date boundary for a game tipping between
    /// midnight and 1am local — which the NBA schedule never does. So the zone is correct in
    /// principle and, for date attribution alone, indistinguishable in practice.
    /// </para>
    /// <para>
    /// The same shortcut pointed the other way is not harmless. If a provider supplies a wall-clock
    /// local time and ingestion applies a fixed offset to derive the instant, the resulting
    /// <see cref="TipOff"/> is an hour off under daylight saving. That does not misfile the day —
    /// it moves the lock, either handing managers an hour of editing after their player has tipped
    /// off, or stealing an hour of legitimate editing before it.
    /// </para>
    /// </remarks>
    public static DateOnly FantasyDateFor(DateTimeOffset tipOff, TimeZoneInfo leagueZone)
    {
        ArgumentNullException.ThrowIfNull(leagueZone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(tipOff, leagueZone).DateTime);
    }
}
