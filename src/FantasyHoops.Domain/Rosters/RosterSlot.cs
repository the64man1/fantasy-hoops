namespace FantasyHoops.Domain.Rosters;

/// <summary>
/// A place on a roster. Distinct from <see cref="PlayerPosition"/>: positions describe a player,
/// slots describe where they are being played.
/// </summary>
public enum RosterSlot
{
    PointGuard,
    ShootingGuard,
    /// <summary>Composite slot accepting either guard position.</summary>
    Guard,
    SmallForward,
    PowerForward,
    /// <summary>Composite slot accepting either forward position.</summary>
    Forward,
    Center,
    /// <summary>Accepts any player regardless of position.</summary>
    Utility,
    Bench,
    InjuredList,
}

public static class RosterSlots
{
    /// <summary>
    /// Slots whose statistics count toward a matchup. Bench and injured list do not.
    /// </summary>
    public static readonly IReadOnlySet<RosterSlot> Active = new HashSet<RosterSlot>
    {
        RosterSlot.PointGuard,
        RosterSlot.ShootingGuard,
        RosterSlot.Guard,
        RosterSlot.SmallForward,
        RosterSlot.PowerForward,
        RosterSlot.Forward,
        RosterSlot.Center,
        RosterSlot.Utility,
    };

    public static bool IsActive(this RosterSlot slot) => Active.Contains(slot);
}
