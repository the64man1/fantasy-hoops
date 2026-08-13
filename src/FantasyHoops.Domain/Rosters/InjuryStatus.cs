namespace FantasyHoops.Domain.Rosters;

/// <summary>
/// A player's availability designation.
/// </summary>
public enum InjuryStatus
{
    Healthy,
    /// <summary>Expected to play but unconfirmed; a game-time decision.</summary>
    GameTimeDecision,
    /// <summary>Ruled out of an upcoming game, but not on a long-term injured list.</summary>
    Out,
    InjuredList,
    /// <summary>Not with the team — G League assignment, personal leave, suspension.</summary>
    NotActive,
}

public static class InjuryStatuses
{
    /// <summary>
    /// Whether a status qualifies a player to occupy an injured list slot.
    /// </summary>
    /// <remarks>
    /// Deliberately excludes <see cref="InjuryStatus.Out"/>. Being ruled out of tonight's game is
    /// not the same as carrying a long-term designation, and letting day-to-day absences occupy
    /// injured list slots would turn them into extra bench spots.
    /// </remarks>
    public static bool QualifiesForInjuredList(this InjuryStatus status) =>
        status is InjuryStatus.InjuredList or InjuryStatus.NotActive;
}
