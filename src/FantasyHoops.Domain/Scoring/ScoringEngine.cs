namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// Resolves head-to-head category matchups.
/// </summary>
/// <remarks>
/// AWAITING IMPLEMENTATION. Behaviour is fully specified by ScoringEngineTests; work against a
/// red suite until it is green. A reference implementation exists on the
/// <c>stash/scoring-implementation</c> branch — consult it only after your own attempt passes,
/// or after you have decided you are stuck.
/// </remarks>
public static class ScoringEngine
{
    /// <summary>
    /// Sums a set of stat lines into team totals. Callers decide which lines belong to a team
    /// for a period; roster and lineup rules are not this engine's concern.
    /// </summary>
    public static CategoryTotals Aggregate(IEnumerable<StatLine> lines) =>
        throw new NotImplementedException();

    /// <summary>
    /// Compares two teams' totals category by category, from <paramref name="team"/>'s perspective.
    /// The result names both teams, so orientation is never inferred from argument order.
    /// </summary>
    public static MatchupResult Resolve(
        TeamTotals team,
        TeamTotals opponent,
        IReadOnlyList<StatCategory>? categories = null) =>
        throw new NotImplementedException();
}
