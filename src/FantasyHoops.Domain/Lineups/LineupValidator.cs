using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Lineups;

/// <summary>
/// Validates lineups in two independent passes: whether the lineup is well formed at all, and
/// whether a change to it is still permitted given which games have started.
/// </summary>
/// <remarks>
/// They are kept apart because they are not two flavours of one question. Shape validity is a
/// predicate on a single lineup; change validity is a predicate on a pair of them. Different
/// arity, and therefore different inputs: shape needs a lineup, a roster and a configuration,
/// while change additionally needs a schedule and a clock. Merging them would force every caller
/// who only wants to know whether an arrangement is legal to manufacture a schedule and a current
/// time in order to ask — and callers with no previous state to diff against (auto-draft output,
/// a seeded lineup, an import, a migration backfill) would have to invent one.
/// <para>
/// Both directions occur. A lineup can be flawless in shape and rejected purely because a player
/// has tipped off — nothing is wrong with it, it is simply too late. Less obviously, a stored
/// lineup can become shape-invalid with nobody touching it: a player parked on the injured list
/// is upgraded to healthy overnight and the arrangement that was legal yesterday no longer is.
/// Shape validity decays on its own, because it depends on facts about players that move
/// underneath it, which is why it has to be answerable with no proposed change in sight.
/// </para>
/// </remarks>
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

        // Rule catches players in the lineup but not on the roster; this one catches players
        // on the roster that the submission gave no slot. Emit RosteredPlayerUnassigned, carrying
        // the PlayerId so the caller can name who was dropped.
        //
        // Closes the omission cheat: a locked player left out of the payload entirely rather than
        // benched. ValidateChange already rejects that on the lock rule (a slot change of
        // something-to-nothing); this is the independent guard at the well-formedness layer, so a
        // malformed submission is refused before locking is even consulted.
        foreach (var player in roster.Values)
        {
            if (!lineup.PlayerIds.Contains(player.PlayerId))
            {
                violations.Add(new LineupViolation(
                    LineupViolationKind.RosteredPlayerUnassigned,
                    "Rostered player is not assigned to any slot.",
                    player.PlayerId));
            }
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
    /// <para>
    /// What makes that airtight is the range rather than the rule: "changed" is evaluated over the
    /// <em>union</em> of both lineups, with absence treated as a legitimate slot value rather than
    /// a gap to skip. Iterating the intersection instead would check precisely the case a cheating
    /// manager will not use — he does not move the player who just went 0-for-6, he omits him.
    /// </para>
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
