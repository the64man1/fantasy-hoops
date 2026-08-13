using FantasyHoops.Domain.Lineups;
using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Tests.Lineups;

public class LineupValidatorTests
{
    private static readonly DateOnly March15 = new(2026, 3, 15);
    private static readonly DateTimeOffset Noon = new(2026, 3, 15, 16, 0, 0, TimeSpan.Zero);

    private static RosteredPlayer Player(
        Guid id,
        PlayerPosition position = PlayerPosition.PointGuard,
        InjuryStatus status = InjuryStatus.Healthy,
        string team = "LAL") =>
        new(id, [position], status, team);

    private static Dictionary<Guid, RosteredPlayer> Roster(params RosteredPlayer[] players) =>
        players.ToDictionary(p => p.PlayerId);

    // -----------------------------------------------------------------
    // Shape
    // -----------------------------------------------------------------

    [Fact]
    public void Shape_AcceptsALegalLineup()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.PointGuard)]);

        var result = LineupValidator.ValidateShape(lineup, Roster(Player(id)), RosterConfiguration.Standard);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Shape_RejectsDuplicatePlayer()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15,
        [
            new SlotAssignment(id, RosterSlot.PointGuard),
            new SlotAssignment(id, RosterSlot.Utility),
        ]);

        var result = LineupValidator.ValidateShape(lineup, Roster(Player(id)), RosterConfiguration.Standard);

        Assert.True(result.HasKind(LineupViolationKind.DuplicatePlayer));
    }

    [Fact]
    public void Shape_RejectsOverfilledSlot()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var lineup = new Lineup(March15,
        [
            new SlotAssignment(a, RosterSlot.PointGuard),
            new SlotAssignment(b, RosterSlot.PointGuard),
        ]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(a), Player(b)), RosterConfiguration.Standard);

        Assert.True(result.HasKind(LineupViolationKind.SlotOverfilled));
    }

    [Fact]
    public void Shape_RejectsPositionIneligibleAssignment()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Center)]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(id, PlayerPosition.PointGuard)), RosterConfiguration.Standard);

        Assert.True(result.HasKind(LineupViolationKind.PositionIneligible));
    }

    [Fact]
    public void Shape_AllowsGuardToFillCompositeGuardSlot()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Guard)]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(id, PlayerPosition.ShootingGuard)), RosterConfiguration.Standard);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Shape_AllowsAnyPositionInUtility()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Utility)]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(id, PlayerPosition.Center)), RosterConfiguration.Standard);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Being ruled out of tonight's game is not a long-term designation. Allowing it into an
    /// injured list slot would quietly convert those slots into extra bench spots.
    /// </summary>
    [Fact]
    public void Shape_RejectsDayToDayPlayerInInjuredListSlot()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.InjuredList)]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(id, status: InjuryStatus.Out)), RosterConfiguration.Standard);

        Assert.True(result.HasKind(LineupViolationKind.InjuredListIneligible));
    }

    [Fact]
    public void Shape_AllowsInjuredListPlayerInInjuredListSlot()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.InjuredList)]);

        var result = LineupValidator.ValidateShape(
            lineup, Roster(Player(id, status: InjuryStatus.InjuredList)), RosterConfiguration.Standard);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Shape_RejectsPlayerNotOnRoster()
    {
        var lineup = new Lineup(March15, [new SlotAssignment(Guid.NewGuid(), RosterSlot.PointGuard)]);

        var result = LineupValidator.ValidateShape(lineup, Roster(), RosterConfiguration.Standard);

        Assert.True(result.HasKind(LineupViolationKind.UnknownPlayer));
    }

    // -----------------------------------------------------------------
    // Change / locking
    // -----------------------------------------------------------------

    private static ScheduledGame GameAt(DateTimeOffset tipOff, string home = "LAL", string away = "BOS") =>
        new(March15, tipOff, home, away);

    [Fact]
    public void Change_AllowsMovingAnUnlockedPlayer()
    {
        var id = Guid.NewGuid();
        var current = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Bench)]);
        var proposed = new Lineup(March15, [new SlotAssignment(id, RosterSlot.PointGuard)]);

        var result = LineupValidator.ValidateChange(
            current, proposed, Roster(Player(id)), [GameAt(Noon.AddHours(4))], Noon);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Change_RejectsMovingALockedPlayer()
    {
        var id = Guid.NewGuid();
        var current = new Lineup(March15, [new SlotAssignment(id, RosterSlot.PointGuard)]);
        var proposed = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Bench)]);

        var result = LineupValidator.ValidateChange(
            current, proposed, Roster(Player(id)), [GameAt(Noon.AddHours(-1))], Noon);

        Assert.True(result.HasKind(LineupViolationKind.PlayerLocked));
    }

    /// <summary>
    /// Adding a locked player is the same retroactive edit as moving one, reached from the other
    /// direction — a manager could otherwise watch a game start well and then insert that player.
    /// </summary>
    [Fact]
    public void Change_RejectsAddingALockedPlayer()
    {
        var id = Guid.NewGuid();
        var current = Lineup.Empty(March15);
        var proposed = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Utility)]);

        var result = LineupValidator.ValidateChange(
            current, proposed, Roster(Player(id)), [GameAt(Noon.AddHours(-1))], Noon);

        Assert.True(result.HasKind(LineupViolationKind.PlayerLocked));
    }

    [Fact]
    public void Change_RejectsRemovingALockedPlayer()
    {
        var id = Guid.NewGuid();
        var current = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Utility)]);
        var proposed = Lineup.Empty(March15);

        var result = LineupValidator.ValidateChange(
            current, proposed, Roster(Player(id)), [GameAt(Noon.AddHours(-1))], Noon);

        Assert.True(result.HasKind(LineupViolationKind.PlayerLocked));
    }

    [Fact]
    public void Change_AllowsMovingAPlayerWithNoGameThatDay()
    {
        var id = Guid.NewGuid();
        var current = new Lineup(March15, [new SlotAssignment(id, RosterSlot.PointGuard)]);
        var proposed = new Lineup(March15, [new SlotAssignment(id, RosterSlot.Bench)]);

        // A game exists, but not for this player's team.
        var result = LineupValidator.ValidateChange(
            current, proposed, Roster(Player(id, team: "LAL")),
            [GameAt(Noon.AddHours(-3), home: "MIA", away: "NYK")], Noon);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Change_IgnoresPlayersWhoseSlotDidNotChange()
    {
        var id = Guid.NewGuid();
        var lineup = new Lineup(March15, [new SlotAssignment(id, RosterSlot.PointGuard)]);

        var result = LineupValidator.ValidateChange(
            lineup, lineup, Roster(Player(id)), [GameAt(Noon.AddHours(-1))], Noon);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// A manager may still edit everyone whose game has not started, even after other players
    /// have locked. Rejecting the whole submission would impose a daily cutoff that does not exist.
    /// </summary>
    [Fact]
    public void Change_AllowsEditingUnlockedPlayersAfterOthersHaveLocked()
    {
        var locked = Guid.NewGuid();
        var free = Guid.NewGuid();

        var current = new Lineup(March15,
        [
            new SlotAssignment(locked, RosterSlot.PointGuard),
            new SlotAssignment(free, RosterSlot.Bench),
        ]);
        var proposed = new Lineup(March15,
        [
            new SlotAssignment(locked, RosterSlot.PointGuard),
            new SlotAssignment(free, RosterSlot.Utility),
        ]);

        var roster = Roster(
            Player(locked, team: "BOS"),
            Player(free, team: "LAL"));

        ScheduledGame[] schedule =
        [
            GameAt(Noon.AddHours(-1), home: "BOS", away: "PHI"),
            GameAt(Noon.AddHours(5), home: "LAL", away: "GSW"),
        ];

        var result = LineupValidator.ValidateChange(current, proposed, roster, schedule, Noon);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Change_RejectsComparingDifferentDates()
    {
        var current = Lineup.Empty(March15);
        var proposed = Lineup.Empty(March15.AddDays(1));

        Assert.Throws<ArgumentException>(() =>
            LineupValidator.ValidateChange(current, proposed, Roster(), [], Noon));
    }
}
