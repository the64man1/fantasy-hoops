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

    // TODO(owner): a slot absent from SlotCounts silently reports capacity 0 rather than erroring.
    // Harmless while Standard is the only configuration. The moment roster shape is commissioner-
    // editable it is a live bug: a config that simply omits Bench turns every bench assignment into
    // an overfill violation, and the manager is shown a nonsense error about a slot they never
    // touched. Two ways out — validate configurations on save so an incomplete one cannot be
    // stored, or make a missing slot explicit here. Owner's call; the first keeps this method total.
    public int Capacity(RosterSlot slot) => SlotCounts.GetValueOrDefault(slot);

    public int ActiveCount => SlotCounts.Where(kv => kv.Key.IsActive()).Sum(kv => kv.Value);

    public int TotalCount => SlotCounts.Values.Sum();
}
