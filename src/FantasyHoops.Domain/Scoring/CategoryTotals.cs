namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// A team's aggregated statistics over a scoring period. Shooting is carried as makes and
/// attempts so percentages stay derivable; see <see cref="StatLine"/> for why.
/// </summary>
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

    /// <summary>
    /// Null when no shots were attempted. A team that never attempted a field goal has no
    /// percentage, which is a different thing from shooting 0%, and collapsing the two would
    /// silently hand the category to an opponent who also never attempted one.
    /// </summary>
    public double? FieldGoalPercentage =>
        FieldGoalsAttempted == 0 ? null : (double)FieldGoalsMade / FieldGoalsAttempted;

    /// <inheritdoc cref="FieldGoalPercentage"/>
    public double? FreeThrowPercentage =>
        FreeThrowsAttempted == 0 ? null : (double)FreeThrowsMade / FreeThrowsAttempted;

    /// <summary>
    /// The comparable value for a category. Null only ever arises from the two percentage
    /// categories; counting stats are always present, with zero being a legitimate total.
    /// </summary>
    public double? ValueOf(StatCategory category) => category switch
    {
        StatCategory.FieldGoalPercentage => FieldGoalPercentage,
        StatCategory.FreeThrowPercentage => FreeThrowPercentage,
        StatCategory.ThreePointersMade => ThreePointersMade,
        StatCategory.Points => Points,
        StatCategory.Rebounds => Rebounds,
        StatCategory.Assists => Assists,
        StatCategory.Steals => Steals,
        StatCategory.Blocks => Blocks,
        StatCategory.Turnovers => Turnovers,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown scoring category."),
    };
}
