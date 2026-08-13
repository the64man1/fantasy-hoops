namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// One player's statistical line for one game.
/// </summary>
/// <remarks>
/// Shooting is stored as makes and attempts, never as a percentage. A team's field goal
/// percentage is total makes over total attempts across every player — not the average of
/// individual percentages. Storing a per-line percentage would make the correct team figure
/// impossible to reconstruct, and averaging percentages is the single most common way to get
/// category scoring quietly wrong.
/// </remarks>
public sealed record StatLine(
    Guid PlayerId,
    DateOnly GameDate,
    int FieldGoalsMade,
    int FieldGoalsAttempted,
    int FreeThrowsMade,
    int FreeThrowsAttempted,
    int ThreePointersMade,
    int Points,
    int Rebounds,
    int Assists,
    int Steals,
    int Blocks,
    int Turnovers)
{
    /// <summary>A line where the player was rostered but did not play.</summary>
    public static StatLine DidNotPlay(Guid playerId, DateOnly gameDate) =>
        new(playerId, gameDate, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
