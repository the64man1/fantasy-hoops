using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Lineups;

/// <summary>
/// Validates lineups in two independent passes: whether the lineup is well formed at all, and
/// whether a change to it is still permitted given which games have started. They are kept apart
/// because a lineup can be perfectly legal in shape yet illegal to submit, and the distinction
/// matters to anything reporting why a change was rejected.
/// </summary>
public static class LineupValidator
{
    /// <summary>
    /// Checks a lineup in isolation: no duplicates, no overfilled slots, every player eligible
    /// for the slot they occupy.
    /// </summary>
    public static LineupValidationResult ValidateShape(
        Lineup lineup,
        IReadOnlyDictionary<Guid, RosteredPlayer> roster,
        RosterConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(configuration);

        var violations = new List<LineupViolation>();

        foreach (var duplicate in lineup.Assignments
                     .GroupBy(a => a.PlayerId)
                     .Where(g => g.Count() > 1))
        {
            violations.Add(new LineupViolation(
                LineupViolationKind.DuplicatePlayer,
                "Player is assigned to more than one slot.",
                duplicate.Key));
        }

        foreach (var overfilled in lineup.Assignments
                     .GroupBy(a => a.Slot)
                     .Where(g => g.Count() > configuration.Capacity(g.Key)))
        {
            violations.Add(new LineupViolation(
                LineupViolationKind.SlotOverfilled,
                $"{overfilled.Count()} players assigned to {overfilled.Key}, which holds {configuration.Capacity(overfilled.Key)}.",
                Slot: overfilled.Key));
        }

        foreach (var assignment in lineup.Assignments)
        {
            if (!roster.TryGetValue(assignment.PlayerId, out var player))
            {
                violations.Add(new LineupViolation(
                    LineupViolationKind.UnknownPlayer,
                    "Player is not on this roster.",
                    assignment.PlayerId,
                    assignment.Slot));
                continue;
            }

            if (!SlotEligibility.CanFillByPosition(assignment.Slot, player.Positions))
            {
                violations.Add(new LineupViolation(
                    LineupViolationKind.PositionIneligible,
                    $"Player is not eligible at {assignment.Slot}.",
                    assignment.PlayerId,
                    assignment.Slot));
            }

            if (assignment.Slot is RosterSlot.InjuredList
                && !player.InjuryStatus.QualifiesForInjuredList())
            {
                violations.Add(new LineupViolation(
                    LineupViolationKind.InjuredListIneligible,
                    $"Player's status ({player.InjuryStatus}) does not qualify for an injured list slot.",
                    assignment.PlayerId,
                    assignment.Slot));
            }
        }

        return new LineupValidationResult(violations);
    }

    /// <summary>
    /// Checks whether moving from <paramref name="current"/> to <paramref name="proposed"/> is
    /// still allowed, given which of the day's games have already tipped off.
    /// </summary>
    /// <remarks>
    /// Any change to a locked player's slot is rejected, in all three forms it can take: moving
    /// them between slots, adding them to a lineup they were absent from, and removing them
    /// entirely. Only checking "moved" would leave the other two as ways to retroactively edit a
    /// player whose game is underway.
    /// </remarks>
    public static LineupValidationResult ValidateChange(
        Lineup current,
        Lineup proposed,
        IReadOnlyDictionary<Guid, RosteredPlayer> roster,
        IEnumerable<ScheduledGame> schedule,
        DateTimeOffset asOf)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(schedule);

        if (current.FantasyDate != proposed.FantasyDate)
        {
            throw new ArgumentException(
                $"Cannot compare lineups for different dates ({current.FantasyDate} and {proposed.FantasyDate}).",
                nameof(proposed));
        }

        var games = schedule as IReadOnlyList<ScheduledGame> ?? schedule.ToList();
        var violations = new List<LineupViolation>();

        var touched = current.PlayerIds.Union(proposed.PlayerIds);

        foreach (var playerId in touched)
        {
            var before = current.SlotOf(playerId);
            var after = proposed.SlotOf(playerId);

            if (before == after) continue;
            if (!roster.TryGetValue(playerId, out var player)) continue; // reported by shape validation

            if (LineupLockPolicy.IsLocked(player, proposed.FantasyDate, games, asOf))
            {
                violations.Add(new LineupViolation(
                    LineupViolationKind.PlayerLocked,
                    "Player's game has started; their slot can no longer be changed.",
                    playerId,
                    after ?? before));
            }
        }

        return new LineupValidationResult(violations);
    }
}
