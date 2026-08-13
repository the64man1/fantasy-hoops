namespace FantasyHoops.Domain.Scoring;

/// <summary>
/// A team's aggregated statistics paired with the team they belong to.
/// </summary>
/// <remarks>
/// Exists so a resolved matchup can state whose result it is. Without it, the orientation of a
/// <see cref="MatchupResult"/> is an implicit contract carried in documentation, and rendering
/// the wrong side inverts an entire matchup with nothing to catch it.
/// </remarks>
public sealed record TeamTotals(Guid TeamId, CategoryTotals Totals);
