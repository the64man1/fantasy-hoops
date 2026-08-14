namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// Resolves head-to-head category matchups.
/// </summary>
/// <remarks>
/// A pure function from stored stat lines to a matchup result, callable at any time for any
/// period. Nothing here accumulates and nothing downstream is stored as the source of truth, so a
/// corrected stat line makes every total, matchup and standing re-derive to the right answer
/// instead of needing to be patched. That property is the reason this subsystem exists.
/// </remarks>
public static class ScoringEngine
{
    /// <summary>
    /// Sums a set of stat lines into team totals. Callers decide which lines belong to a team
    /// for a period; roster and lineup rules are not this engine's concern.
    /// </summary>
    public static CategoryTotals Aggregate(IEnumerable<StatLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        // Materialised once. The sums below each walk the sequence, so a deferred query would
        // otherwise be executed eleven times, and a sequence that cannot be replayed would report
        // zero for ten of the eleven categories. A collection already in memory is used as it is.
        var stats = lines as IReadOnlyCollection<StatLine> ?? lines.ToList();

        return new CategoryTotals(
            stats.Sum(line => line.FieldGoalsMade),
            stats.Sum(line => line.FieldGoalsAttempted),
            stats.Sum(line => line.FreeThrowsMade),
            stats.Sum(line => line.FreeThrowsAttempted),
            stats.Sum(line => line.ThreePointersMade),
            stats.Sum(line => line.Points),
            stats.Sum(line => line.Rebounds),
            stats.Sum(line => line.Assists),
            stats.Sum(line => line.Steals),
            stats.Sum(line => line.Blocks),
            stats.Sum(line => line.Turnovers));
    }

    /// <summary>
    /// Compares two teams' totals category by category, from <paramref name="team"/>'s perspective.
    /// The result names both teams, so orientation is never inferred from argument order.
    /// </summary>
    public static MatchupResult Resolve(
        TeamTotals team,
        TeamTotals opponent,
        IReadOnlyList<StatCategory>? categories = null)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(opponent);
        if (team.TeamId == opponent.TeamId) throw new ArgumentException("A team cannot play itself.", nameof(opponent));

        var categoryOutcomes = new Dictionary<StatCategory, CategoryOutcome>();
        foreach (var category in categories ?? StatCategories.NineCategory)
        {
            SetCategoryOutcome(team.Totals, opponent.Totals, category, categoryOutcomes);
        }

        return new MatchupResult(team.TeamId, opponent.TeamId, categoryOutcomes);
    }

    private static void SetCategoryOutcome(
        CategoryTotals teamTotals,
        CategoryTotals opponentTotals,
        StatCategory statCategory,
        Dictionary<StatCategory, CategoryOutcome> categories)
    {
        var teamCategoryTotal = teamTotals.ValueOf(statCategory);
        var opponentCategoryTotal = opponentTotals.ValueOf(statCategory);

        if (teamCategoryTotal == opponentCategoryTotal)
        {
            categories.Add(statCategory, CategoryOutcome.Tie);
            return;
        }

        if (teamCategoryTotal == null)
        {
            categories.Add(statCategory, CategoryOutcome.Loss);
            return;
        }

        if (opponentCategoryTotal == null)
        {
            categories.Add(statCategory, CategoryOutcome.Win);
            return;
        }

        CategoryOutcome outcome;
        if (StatCategories.LowerIsBetter(statCategory))
        {
            outcome = teamCategoryTotal < opponentCategoryTotal
                ? CategoryOutcome.Win
                : CategoryOutcome.Loss;
        }
        else
        {
            outcome = teamCategoryTotal > opponentCategoryTotal
                ? CategoryOutcome.Win
                : CategoryOutcome.Loss;
        }

        categories.Add(statCategory, outcome);
    }
}
