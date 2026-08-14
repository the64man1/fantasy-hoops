using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Lineups;

/// <summary>
/// Decides whether a player's roster slot can still be changed.
/// </summary>
/// <remarks>
/// Locking is per player, not per day. A manager can keep editing everyone whose game has not
/// started, right up until each individual tip-off — there is no single daily cutoff. Getting
/// this wrong in either direction is costly: too coarse and managers lose hours of legitimate
/// editing time, too loose and they can retroactively bench a player who already scored.
/// </remarks>
public static class LineupLockPolicy
{
    /// <summary>
    /// Whether a player is locked for a fantasy date.
    /// </summary>
    /// <remarks>
    /// A player with no game that day is never locked; there is nothing to protect against.
    /// Streaming is the obvious beneficiary, but drops are where a daily cutoff would actually
    /// hurt: a manager who cannot drop an idle player cannot pick anyone up either, and the
    /// waiver clock does not care that it is 11pm. Additionally, as a courtesy, we are allowing
    /// to submit a roster change if they happen to submit a change the exact tip-off time tick.
    /// </remarks>
    public static bool IsLocked(ScheduledGame? game, DateTimeOffset asOf) =>
        game is not null && asOf > game.TipOff;

    /// <summary>
    /// Finds a player's game for a fantasy date, if they have one.
    /// </summary>
    /// <remarks>
    /// TODO(owner): this takes the first match. A team cannot play twice on one fantasy date, so
    /// duplicates only arise from ingestion — the same game arriving twice, or a reschedule landing
    /// alongside the row it replaces. Identical tip-offs make the choice moot; disagreeing ones do
    /// not, and the conservative reading is that the earliest tip-off locks. Worth settling
    /// alongside the week-2 re-poll path rather than here.
    /// </remarks>
    public static ScheduledGame? GameFor(
        RosteredPlayer player,
        DateOnly fantasyDate,
        IEnumerable<ScheduledGame> schedule)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(schedule);

        return schedule.FirstOrDefault(g => g.FantasyDate == fantasyDate && g.Involves(player.NbaTeam));
    }

    public static bool IsLocked(
        RosteredPlayer player,
        DateOnly fantasyDate,
        IEnumerable<ScheduledGame> schedule,
        DateTimeOffset asOf) =>
        IsLocked(GameFor(player, fantasyDate, schedule), asOf);
}
