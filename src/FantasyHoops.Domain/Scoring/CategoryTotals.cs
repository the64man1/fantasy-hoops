namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// A team's aggregated statistics over a scoring period.
/// </summary>
/// <remarks>
/// Note what this type carries and, more importantly, what it does not. Makes and attempts are
/// stored separately and the percentages are derived, because a percentage cannot be summed:
/// a team going 1-for-1 and 0-for-9 shot 10%, not 50%. Storing the ratio instead of its parts
/// would make aggregation lossy in a way no later code could recover from.
/// <para>
/// The percentages are <c>null</c> rather than zero when nothing was attempted, which is a
/// different fact from attempting and missing. Comparisons must resolve that distinction before
/// comparing, since neither <c>&gt;</c> nor <c>&lt;</c> is true against a null.
/// </para>
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

    public double? FieldGoalPercentage => FieldGoalsAttempted > 0 ? (double)FieldGoalsMade / FieldGoalsAttempted : null;

    public double? FreeThrowPercentage => FreeThrowsAttempted > 0 ? (double)FreeThrowsMade / FreeThrowsAttempted : null;

    /// <summary>The comparable value for a category.</summary>
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
        _ => throw new ArgumentException("StatCategory invalid", nameof(category))
    };
}
