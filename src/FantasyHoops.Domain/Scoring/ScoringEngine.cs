namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// Resolves head-to-head category matchups.
/// </summary>
/// <remarks>
/// Deliberately a pure function over stat lines with no persistence, clock, or configuration
/// state. Any past matchup can be recomputed at any time from the lines alone, which is what
/// makes late-arriving stat corrections recoverable: correct the line, recompute, done. An
/// engine that accumulated running totals would instead drift silently out of sync.
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

        var totals = CategoryTotals.Empty;

        foreach (var line in lines)
        {
            totals = totals with
            {
                FieldGoalsMade = totals.FieldGoalsMade + line.FieldGoalsMade,
                FieldGoalsAttempted = totals.FieldGoalsAttempted + line.FieldGoalsAttempted,
                FreeThrowsMade = totals.FreeThrowsMade + line.FreeThrowsMade,
                FreeThrowsAttempted = totals.FreeThrowsAttempted + line.FreeThrowsAttempted,
                ThreePointersMade = totals.ThreePointersMade + line.ThreePointersMade,
                Points = totals.Points + line.Points,
                Rebounds = totals.Rebounds + line.Rebounds,
                Assists = totals.Assists + line.Assists,
                Steals = totals.Steals + line.Steals,
                Blocks = totals.Blocks + line.Blocks,
                Turnovers = totals.Turnovers + line.Turnovers,
            };
        }

        return totals;
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

        if (team.TeamId == opponent.TeamId)
            throw new ArgumentException("A team cannot play itself.", nameof(opponent));

        var scored = categories ?? StatCategories.NineCategory;

        return new MatchupResult(
            team.TeamId,
            opponent.TeamId,
            scored.ToDictionary(
                category => category,
                category => Compare(
                    category,
                    team.Totals.ValueOf(category),
                    opponent.Totals.ValueOf(category))));
    }

    private static CategoryOutcome Compare(StatCategory category, double? mine, double? theirs)
    {
        // Null means no shot attempts at all, which only the percentage categories can produce.
        // Neither side attempting anything is a tie; a side that attempted nothing cannot beat
        // one that did.
        //
        // Yahoo's exact handling of this case is unverified, so it is isolated here rather than
        // scattered through the comparison. Worth confirming against a real league before the
        // season, though it requires a team to field nobody for a full period to occur at all.
        if (mine is null && theirs is null) return CategoryOutcome.Tie;
        if (mine is null) return CategoryOutcome.Loss;
        if (theirs is null) return CategoryOutcome.Win;

        var comparison = mine.Value.CompareTo(theirs.Value);
        if (comparison == 0) return CategoryOutcome.Tie;

        var iAmHigher = comparison > 0;
        var higherWins = !category.LowerIsBetter();

        return iAmHigher == higherWins ? CategoryOutcome.Win : CategoryOutcome.Loss;
    }
}
