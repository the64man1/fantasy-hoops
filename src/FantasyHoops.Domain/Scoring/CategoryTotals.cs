namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// A team's aggregated statistics over a scoring period.
/// </summary>
/// <remarks>
/// The record shape is settled; the derived members below are AWAITING IMPLEMENTATION and are
/// specified by ScoringEngineTests. Note what this type carries and, more importantly, what it
/// deliberately does not.
/// </remarks>
public sealed record CategoryTotals(
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
    public static readonly CategoryTotals Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public double? FieldGoalPercentage => throw new NotImplementedException();

    public double? FreeThrowPercentage => throw new NotImplementedException();

    /// <summary>The comparable value for a category.</summary>
    public double? ValueOf(StatCategory category) => throw new NotImplementedException();
}
