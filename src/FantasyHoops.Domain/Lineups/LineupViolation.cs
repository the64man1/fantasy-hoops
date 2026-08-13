using FantasyHoops.Domain.Rosters;

namespace FantasyHoops.Domain.Lineups;

public enum LineupViolationKind
{
    /// <summary>The same player appears in more than one slot.</summary>
    DuplicatePlayer,
    /// <summary>More players assigned to a slot than the league carries.</summary>
    SlotOverfilled,
    /// <summary>The player's positions do not permit the slot.</summary>
    PositionIneligible,
    /// <summary>The player's status does not qualify them for an injured list slot.</summary>
    InjuredListIneligible,
    /// <summary>The player is unknown to the roster.</summary>
    UnknownPlayer,
    /// <summary>The player's game has started, so their slot can no longer change.</summary>
    PlayerLocked,
}

public sealed record LineupViolation(
    LineupViolationKind Kind,
    string Message,
    Guid? PlayerId = null,
    RosterSlot? Slot = null);

public sealed record LineupValidationResult(IReadOnlyList<LineupViolation> Violations)
{
    public static readonly LineupValidationResult Valid = new([]);

    public bool IsValid => Violations.Count == 0;

    public bool HasKind(LineupViolationKind kind) => Violations.Any(v => v.Kind == kind);
}
