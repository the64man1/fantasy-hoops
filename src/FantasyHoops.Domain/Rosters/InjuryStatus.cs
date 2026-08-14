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
    /// <para>
    /// The competitive effect is the reason this is not a preference. Admitting <c>Out</c> lets a
    /// manager wait for the designation to drop, park the player, stream a body into the vacated
    /// active slot, and un-park him two days later — free roster expansion for whoever checks their
    /// phone at 6pm, and a standing bonus to rest-prone veterans. Changing it is a rules change the
    /// league has to be told about, not a settings toggle.
    /// </para>
    /// <para>
    /// This rule is only as good as the status mapping feeding it. Providers frequently collapse
    /// "out tonight" and "out for the season" into one value; verify what the chosen provider
    /// actually distinguishes before trusting <see cref="InjuryStatus.Out"/> to mean what it says.
    /// </para>
    /// </remarks>
    public static bool QualifiesForInjuredList(this InjuryStatus status) =>
        status is InjuryStatus.InjuredList or InjuryStatus.NotActive;
}
