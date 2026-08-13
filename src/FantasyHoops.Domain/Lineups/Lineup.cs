using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Lineups;

public sealed record SlotAssignment(Guid PlayerId, RosterSlot Slot);

/// <summary>
/// One team's slot assignments for a single fantasy day.
/// </summary>
public sealed record Lineup(DateOnly FantasyDate, IReadOnlyList<SlotAssignment> Assignments)
{
    public static Lineup Empty(DateOnly fantasyDate) => new(fantasyDate, []);

    public RosterSlot? SlotOf(Guid playerId) =>
        Assignments.FirstOrDefault(a => a.PlayerId == playerId)?.Slot;

    public IEnumerable<Guid> PlayerIds => Assignments.Select(a => a.PlayerId);

    /// <summary>Players whose statistics count for this day.</summary>
    public IEnumerable<Guid> ActivePlayerIds =>
        Assignments.Where(a => a.Slot.IsActive()).Select(a => a.PlayerId);
}
