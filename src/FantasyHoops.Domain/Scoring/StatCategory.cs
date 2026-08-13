namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// The nine scoring categories in standard head-to-head fantasy basketball.
/// Eight-category leagues are the same set without <see cref="Turnovers"/>.
/// </summary>
public enum StatCategory
{
    FieldGoalPercentage,
    FreeThrowPercentage,
    ThreePointersMade,
    Points,
    Rebounds,
    Assists,
    Steals,
    Blocks,
    Turnovers,
}

public static class StatCategories
{
    public static readonly IReadOnlyList<StatCategory> NineCategory =
    [
        StatCategory.FieldGoalPercentage,
        StatCategory.FreeThrowPercentage,
        StatCategory.ThreePointersMade,
        StatCategory.Points,
        StatCategory.Rebounds,
        StatCategory.Assists,
        StatCategory.Steals,
        StatCategory.Blocks,
        StatCategory.Turnovers,
    ];

    public static readonly IReadOnlyList<StatCategory> EightCategory =
        NineCategory.Where(c => c != StatCategory.Turnovers).ToArray();

    /// <summary>
    /// Turnovers are the only category where a lower total is better. Every comparison
    /// must consult this rather than assuming higher wins.
    /// </summary>
    public static bool LowerIsBetter(this StatCategory category) =>
        category == StatCategory.Turnovers;
}
