namespace FantasyHoops.Domain.Scoring;

public enum CategoryOutcome
{
    Win,
    Loss,
    Tie,
}

/// <summary>
/// The outcome of one head-to-head matchup, stated from the perspective of a single team.
/// </summary>
public sealed record MatchupResult(IReadOnlyDictionary<StatCategory, CategoryOutcome> Categories)
{
    public int Wins => Categories.Values.Count(o => o == CategoryOutcome.Win);
    public int Losses => Categories.Values.Count(o => o == CategoryOutcome.Loss);
    public int Ties => Categories.Values.Count(o => o == CategoryOutcome.Tie);

    /// <summary>The result as the opposing team would see it.</summary>
    public MatchupResult Invert() => new(
        Categories.ToDictionary(
            kv => kv.Key,
            kv => kv.Value switch
            {
                CategoryOutcome.Win => CategoryOutcome.Loss,
                CategoryOutcome.Loss => CategoryOutcome.Win,
                _ => CategoryOutcome.Tie,
            }));

    // The compiler-generated record equality compares Categories by reference, because that is
    // what EqualityComparer<T>.Default does for a dictionary. Two results with identical contents
    // would therefore compare unequal — which would make "did recomputing this matchup change the
    // outcome?" answer yes every single time, and quietly defeat correction handling.
    public bool Equals(MatchupResult? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Categories.Count != other.Categories.Count) return false;

        foreach (var (category, outcome) in Categories)
        {
            if (!other.Categories.TryGetValue(category, out var theirs) || theirs != outcome)
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        // Ordered so hashing does not depend on dictionary enumeration order.
        var hash = new HashCode();
        foreach (var (category, outcome) in Categories.OrderBy(kv => kv.Key))
        {
            hash.Add(category);
            hash.Add(outcome);
        }
        return hash.ToHashCode();
    }
}
