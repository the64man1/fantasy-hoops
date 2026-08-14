using FantasyHoops.Domain.Lineups;
using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Tests.Lineups;

public class LineupLockPolicyTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly DateOnly March15 = new(2026, 3, 15);

    private static RosteredPlayer Player(string team = "LAL") =>
        new(Guid.NewGuid(), [PlayerPosition.PointGuard], InjuryStatus.Healthy, team);

    private static ScheduledGame Game(DateTimeOffset tipOff, string home = "LAL", string away = "BOS") =>
        new(ScheduledGame.FantasyDateFor(tipOff, Eastern), tipOff, home, away);

    // -----------------------------------------------------------------
    // Fantasy date attribution
    // -----------------------------------------------------------------

    /// <summary>
    /// The bug this design exists to prevent. A 10:30pm Eastern tip-off on March 15 is 02:30 UTC
    /// on March 16. Reading the date off the instant attributes the game to the wrong fantasy day,
    /// which silently moves a player's statistics into the following day — and, across a week
    /// boundary, into the following matchup.
    /// </summary>
    [Fact]
    public void FantasyDate_ForLateTipOff_IsTheEasternDate_NotTheUtcDate()
    {
        var tipOff = new DateTimeOffset(2026, 3, 16, 2, 30, 0, TimeSpan.Zero); // 10:30pm ET Mar 15

        Assert.Equal(new DateOnly(2026, 3, 16), DateOnly.FromDateTime(tipOff.UtcDateTime));
        Assert.Equal(March15, ScheduledGame.FantasyDateFor(tipOff, Eastern));
    }

    [Fact]
    public void FantasyDate_ForAfternoonTipOff_MatchesTheCalendarDate()
    {
        var tipOff = new DateTimeOffset(2026, 3, 15, 19, 0, 0, TimeSpan.Zero); // 3:00pm ET

        Assert.Equal(March15, ScheduledGame.FantasyDateFor(tipOff, Eastern));
    }

    /// <summary>
    /// Daylight saving in the United States began on 8 March 2026. A fixed UTC offset would put
    /// this game on the wrong day; resolving through the zone handles the transition.
    /// </summary>
    [Fact]
    public void FantasyDate_HandlesDaylightSavingTransition()
    {
        var beforeDst = new DateTimeOffset(2026, 3, 7, 1, 0, 0, TimeSpan.Zero);  // 8:00pm EST Mar 6
        var afterDst = new DateTimeOffset(2026, 3, 14, 1, 0, 0, TimeSpan.Zero);  // 9:00pm EDT Mar 13

        Assert.Equal(new DateOnly(2026, 3, 6), ScheduledGame.FantasyDateFor(beforeDst, Eastern));
        Assert.Equal(new DateOnly(2026, 3, 13), ScheduledGame.FantasyDateFor(afterDst, Eastern));
    }

    // -----------------------------------------------------------------
    // Locking
    // -----------------------------------------------------------------

    [Fact]
    public void NotLocked_BeforeTipOff()
    {
        var tipOff = new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero);

        Assert.False(LineupLockPolicy.IsLocked(Game(tipOff), tipOff.AddSeconds(-1)));
    }

    [Fact]
    public void NotLocked_AtTipOff()
    {
        var tipOff = new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero);

        Assert.False(LineupLockPolicy.IsLocked(Game(tipOff), tipOff));
    }

    [Fact]
    public void Locked_AfterTipOff()
    {
        var tipOff = new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero);

        Assert.True(LineupLockPolicy.IsLocked(Game(tipOff), tipOff.AddHours(2)));
    }

    /// <summary>
    /// Players without a game are freely movable all day. Streaming depends on this — managers
    /// rotate players through off days constantly.
    /// </summary>
    [Fact]
    public void NeverLocked_WhenThePlayerHasNoGame()
    {
        var player = Player("LAL");
        var somebodyElsesGame = Game(new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero), home: "MIA", away: "NYK");

        var locked = LineupLockPolicy.IsLocked(player, March15, [somebodyElsesGame], new DateTimeOffset(2026, 3, 16, 6, 0, 0, TimeSpan.Zero));

        Assert.False(locked);
    }

    /// <summary>
    /// Locking is per player, not per day: an early game locking one player must leave a
    /// teammate-of-nobody with a later tip-off still editable.
    /// </summary>
    [Fact]
    public void LocksIndependently_PerPlayer_WithinTheSameDay()
    {
        var earlyPlayer = Player("BOS");
        var latePlayer = Player("LAL");

        var early = Game(new DateTimeOffset(2026, 3, 15, 17, 0, 0, TimeSpan.Zero), home: "BOS", away: "PHI");
        var late = Game(new DateTimeOffset(2026, 3, 16, 2, 30, 0, TimeSpan.Zero), home: "LAL", away: "GSW");

        // Between the two tip-offs.
        var now = new DateTimeOffset(2026, 3, 15, 20, 0, 0, TimeSpan.Zero);
        ScheduledGame[] schedule = [early, late];

        Assert.True(LineupLockPolicy.IsLocked(earlyPlayer, March15, schedule, now));
        Assert.False(LineupLockPolicy.IsLocked(latePlayer, March15, schedule, now));
    }

    [Fact]
    public void FindsGame_RegardlessOfHomeOrAway()
    {
        var home = Player("LAL");
        var away = Player("GSW");
        var game = Game(new DateTimeOffset(2026, 3, 15, 23, 0, 0, TimeSpan.Zero), home: "LAL", away: "GSW");

        Assert.NotNull(LineupLockPolicy.GameFor(home, March15, [game]));
        Assert.NotNull(LineupLockPolicy.GameFor(away, March15, [game]));
    }

    [Fact]
    public void IgnoresGamesOnOtherDates()
    {
        var player = Player("LAL");
        var yesterday = Game(new DateTimeOffset(2026, 3, 14, 23, 0, 0, TimeSpan.Zero));

        Assert.Null(LineupLockPolicy.GameFor(player, March15, [yesterday]));
    }
}
