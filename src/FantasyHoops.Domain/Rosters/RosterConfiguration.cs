namespace FantasyHoops.Domain.Rosters;

/// <summary>
/// How many of each slot a league's rosters carry.
/// </summary>
public sealed record RosterConfiguration(IReadOnlyDictionary<RosterSlot, int> SlotCounts)
{
    /// <summary>A conventional head-to-head roster: ten active, three bench, three injured list.</summary>
    public static readonly RosterConfiguration Standard = new(new Dictionary<RosterSlot, int>
    {
        [RosterSlot.PointGuard] = 1,
        [RosterSlot.ShootingGuard] = 1,
        [RosterSlot.Guard] = 1,
        [RosterSlot.SmallForward] = 1,
        [RosterSlot.PowerForward] = 1,
        [RosterSlot.Forward] = 1,
        [RosterSlot.Center] = 2,
        [RosterSlot.Utility] = 2,
        [RosterSlot.Bench] = 3,
        [RosterSlot.InjuredList] = 3,
    });

    public int Capacity(RosterSlot slot) => SlotCounts.GetValueOrDefault(slot);

    public int ActiveCount => SlotCounts.Where(kv => kv.Key.IsActive()).Sum(kv => kv.Value);

    public int TotalCount => SlotCounts.Values.Sum();
}
