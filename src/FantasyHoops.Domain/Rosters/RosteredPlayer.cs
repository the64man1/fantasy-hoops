namespace FantasyHoops.Domain.Rosters;

/// <summary>
/// The facts about a player that lineup rules depend on.
/// </summary>
/// <param name="NbaTeam">
/// Team abbreviation, used to find whether the player has a game on a given date and therefore
/// whether their slot has locked.
/// </param>
public sealed record RosteredPlayer(
    Guid PlayerId,
    IReadOnlyCollection<PlayerPosition> Positions,
    InjuryStatus InjuryStatus,
    string NbaTeam);
