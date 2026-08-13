namespace FantasyHoops.Domain.Rosters;

public static class SlotEligibility
{
    private static readonly Dictionary<RosterSlot, PlayerPosition[]> AcceptedPositions = new()
    {
        [RosterSlot.PointGuard] = [PlayerPosition.PointGuard],
        [RosterSlot.ShootingGuard] = [PlayerPosition.ShootingGuard],
        [RosterSlot.Guard] = [PlayerPosition.PointGuard, PlayerPosition.ShootingGuard],
        [RosterSlot.SmallForward] = [PlayerPosition.SmallForward],
        [RosterSlot.PowerForward] = [PlayerPosition.PowerForward],
        [RosterSlot.Forward] = [PlayerPosition.SmallForward, PlayerPosition.PowerForward],
        [RosterSlot.Center] = [PlayerPosition.Center],
    };

    /// <summary>
    /// Whether a player's positions permit them to occupy a slot.
    /// </summary>
    /// <remarks>
    /// Position only. Injured list eligibility depends on a player's status rather than where
    /// they play, so it is checked separately via
    /// <see cref="InjuryStatuses.QualifiesForInjuredList"/>.
    /// </remarks>
    public static bool CanFillByPosition(RosterSlot slot, IReadOnlyCollection<PlayerPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        // Utility, bench, and the injured list impose no positional requirement.
        if (slot is RosterSlot.Utility or RosterSlot.Bench or RosterSlot.InjuredList) return true;

        return AcceptedPositions.TryGetValue(slot, out var accepted)
            && positions.Any(accepted.Contains);
    }
}
