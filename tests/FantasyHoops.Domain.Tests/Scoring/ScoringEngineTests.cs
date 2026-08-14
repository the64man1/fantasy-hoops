using FantasyHoops.Domain.Scoring;

namespace FantasyHoops.Domain.Tests.Scoring;

public class ScoringEngineTests
{
    private static readonly DateOnly AnyDate = new(2026, 3, 15);
    private static readonly Guid TeamA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TeamB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static StatLine Line(
        int fgm = 0, int fga = 0, int ftm = 0, int fta = 0, int fg3m = 0,
        int pts = 0, int reb = 0, int ast = 0, int stl = 0, int blk = 0, int tov = 0) =>
        new(Guid.NewGuid(), AnyDate, fgm, fga, ftm, fta, fg3m, pts, reb, ast, stl, blk, tov);

    private static CategoryTotals Totals(
        int fgm = 0, int fga = 0, int ftm = 0, int fta = 0, int fg3m = 0,
        int pts = 0, int reb = 0, int ast = 0, int stl = 0, int blk = 0, int tov = 0) =>
        new(fgm, fga, ftm, fta, fg3m, pts, reb, ast, stl, blk, tov);

    private static TeamTotals A(CategoryTotals totals) => new(TeamA, totals);
    private static TeamTotals B(CategoryTotals totals) => new(TeamB, totals);

    // ---------------------------------------------------------------------
    // Aggregation
    // ---------------------------------------------------------------------

    [Fact]
    public void Aggregate_SumsCountingCategories()
    {
        var totals = ScoringEngine.Aggregate(
        [
            Line(pts: 20, reb: 10, ast: 5, stl: 2, blk: 1, tov: 3, fg3m: 2),
            Line(pts: 15, reb: 4, ast: 9, stl: 1, blk: 0, tov: 2, fg3m: 4),
        ]);

        Assert.Equal(35, totals.Points);
        Assert.Equal(14, totals.Rebounds);
        Assert.Equal(14, totals.Assists);
        Assert.Equal(3, totals.Steals);
        Assert.Equal(1, totals.Blocks);
        Assert.Equal(5, totals.Turnovers);
        Assert.Equal(6, totals.ThreePointersMade);
    }

    [Fact]
    public void Aggregate_OfNothing_IsEmpty()
    {
        Assert.Equal(CategoryTotals.Empty, ScoringEngine.Aggregate([]));
    }

    /// <summary>
    /// The bug this whole design exists to prevent. One player going 1-for-1 and another going
    /// 0-for-9 is 1-for-10 as a team, which is 10% — not the 50% you get by averaging 100% and 0%.
    /// </summary>
    [Fact]
    public void Aggregate_PercentagesUseTotalMakesOverTotalAttempts_NotTheAverageOfPercentages()
    {
        var totals = ScoringEngine.Aggregate(
        [
            Line(fgm: 1, fga: 1),
            Line(fgm: 0, fga: 9),
        ]);

        Assert.Equal(0.10, totals.FieldGoalPercentage!.Value, precision: 10);
        Assert.NotEqual(0.50, totals.FieldGoalPercentage!.Value, precision: 10);
    }

    [Fact]
    public void Aggregate_TreatsDidNotPlayAsContributingNothing()
    {
        var played = Line(fgm: 5, fga: 10, pts: 12);

        var withDnp = ScoringEngine.Aggregate([played, StatLine.DidNotPlay(Guid.NewGuid(), AnyDate)]);
        var withoutDnp = ScoringEngine.Aggregate([played]);

        Assert.Equal(withoutDnp, withDnp);
    }

    // ---------------------------------------------------------------------
    // Percentage edge cases
    // ---------------------------------------------------------------------

    [Fact]
    public void Percentage_WithNoAttempts_IsNullRatherThanZero()
    {
        var totals = ScoringEngine.Aggregate([Line(pts: 2)]);

        Assert.Null(totals.FieldGoalPercentage);
        Assert.Null(totals.FreeThrowPercentage);
    }

    [Fact]
    public void Percentage_MissingEverything_IsZeroNotNull()
    {
        var totals = ScoringEngine.Aggregate([Line(fgm: 0, fga: 8)]);

        Assert.Equal(0.0, totals.FieldGoalPercentage!.Value, precision: 10);
    }

    /// <summary>
    /// Stat lines arrive as a query, not a list, once persistence lands. Enumerating the sequence
    /// once per category means eleven passes over it — eleven round trips against a deferred
    /// LINQ query, and outright wrong answers for any sequence that cannot be replayed.
    /// </summary>
    [Fact]
    public void Aggregate_EnumeratesTheSequenceOnlyOnce()
    {
        var passes = 0;

        IEnumerable<StatLine> Lines()
        {
            passes++;
            yield return Line(fgm: 2, fga: 4, pts: 10, reb: 3);
            yield return Line(fgm: 1, fga: 5, pts: 4, reb: 2);
        }

        var totals = ScoringEngine.Aggregate(Lines());

        Assert.Equal(14, totals.Points);
        Assert.Equal(1, passes);
    }

    // ---------------------------------------------------------------------
    // Category comparison
    // ---------------------------------------------------------------------

    [Fact]
    public void Resolve_HigherWins_ForNormalCategories()
    {
        var result = ScoringEngine.Resolve(A(Totals(pts: 500)), B(Totals(pts: 400)));

        Assert.Equal(CategoryOutcome.Win, result.Categories[StatCategory.Points]);
    }

    /// <summary>Turnovers invert: fewer is better. Easy to get backwards, so it is asserted directly.</summary>
    [Fact]
    public void Resolve_LowerWins_ForTurnovers()
    {
        var fewer = ScoringEngine.Resolve(A(Totals(tov: 40)), B(Totals(tov: 60)));
        var more = ScoringEngine.Resolve(A(Totals(tov: 60)), B(Totals(tov: 40)));

        Assert.Equal(CategoryOutcome.Win, fewer.Categories[StatCategory.Turnovers]);
        Assert.Equal(CategoryOutcome.Loss, more.Categories[StatCategory.Turnovers]);
    }

    [Fact]
    public void Resolve_EqualTotals_Tie()
    {
        var result = ScoringEngine.Resolve(A(Totals(reb: 100, tov: 30)), B(Totals(reb: 100, tov: 30)));

        Assert.Equal(CategoryOutcome.Tie, result.Categories[StatCategory.Rebounds]);
        Assert.Equal(CategoryOutcome.Tie, result.Categories[StatCategory.Turnovers]);
    }

    [Fact]
    public void Resolve_NeitherTeamAttempted_IsTie()
    {
        var result = ScoringEngine.Resolve(A(Totals(fga: 0)), B(Totals(fga: 0)));

        Assert.Equal(CategoryOutcome.Tie, result.Categories[StatCategory.FieldGoalPercentage]);
    }

    [Fact]
    public void Resolve_NoAttemptsLosesToAnyAttempts_EvenWhenTheOpponentMissedEverything()
    {
        var result = ScoringEngine.Resolve(
            A(Totals(fgm: 0, fga: 0)),
            B(Totals(fgm: 0, fga: 20)));

        Assert.Equal(CategoryOutcome.Loss, result.Categories[StatCategory.FieldGoalPercentage]);
    }

    /// <summary>
    /// The mirror of the case above, which is the orientation the original test never checked.
    /// A team that attempted and missed everything still did something; a team that never attempted
    /// did not. Whichever way round the arguments arrive, attempts beat no attempts.
    /// </summary>
    [Fact]
    public void Resolve_AnyAttemptsBeatsNoAttempts_WhicheverSideIsAsking()
    {
        var result = ScoringEngine.Resolve(
            A(Totals(fgm: 0, fga: 20)),
            B(Totals(fgm: 0, fga: 0)));

        Assert.Equal(CategoryOutcome.Win, result.Categories[StatCategory.FieldGoalPercentage]);
    }

    /// <summary>
    /// Symmetry specifically across a null-versus-value comparison. The existing symmetry test
    /// gives both teams attempts, so it cannot detect a comparison that collapses to the same
    /// answer in both directions.
    /// </summary>
    [Fact]
    public void Resolve_IsSymmetric_WhenOneTeamNeverAttempted()
    {
        var attempted = Totals(fgm: 4, fga: 9);
        var never = Totals(fgm: 0, fga: 0);

        var forward = ScoringEngine.Resolve(A(attempted), B(never));
        var backward = ScoringEngine.Resolve(B(never), A(attempted));

        Assert.Equal(forward.Invert(), backward);
    }

    // ---------------------------------------------------------------------
    // Whole matchups
    // ---------------------------------------------------------------------

    [Fact]
    public void Resolve_ScoresAllNineCategories()
    {
        var result = ScoringEngine.Resolve(A(Totals()), B(Totals()));

        Assert.Equal(9, result.Categories.Count);
        Assert.Equal(9, result.Ties);
    }

    [Fact]
    public void Resolve_EightCategory_OmitsTurnovers()
    {
        var result = ScoringEngine.Resolve(A(Totals()), B(Totals()), StatCategories.EightCategory);

        Assert.Equal(8, result.Categories.Count);
        Assert.DoesNotContain(StatCategory.Turnovers, result.Categories.Keys);
    }

    [Fact]
    public void Resolve_TalliesWinsLossesAndTies()
    {
        var team = Totals(fgm: 5, fga: 10, ftm: 8, fta: 10, fg3m: 10, pts: 500, reb: 200, ast: 100, stl: 50, blk: 20, tov: 40);
        var opponent = Totals(fgm: 3, fga: 10, ftm: 9, fta: 10, fg3m: 10, pts: 400, reb: 250, ast: 90, stl: 40, blk: 30, tov: 60);

        var result = ScoringEngine.Resolve(A(team), B(opponent));

        // Won: FG%, PTS, AST, STL, TOV. Tied: 3PM. Lost: FT%, REB, BLK.
        Assert.Equal(5, result.Wins);
        Assert.Equal(3, result.Losses);
        Assert.Equal(1, result.Ties);
        Assert.Equal(9, result.Wins + result.Losses + result.Ties);
    }

    // ---------------------------------------------------------------------
    // Orientation
    // ---------------------------------------------------------------------

    [Fact]
    public void Resolve_NamesBothTeams()
    {
        var result = ScoringEngine.Resolve(A(Totals(pts: 500)), B(Totals(pts: 400)));

        Assert.Equal(TeamA, result.TeamId);
        Assert.Equal(TeamB, result.OpponentId);
    }

    [Fact]
    public void Invert_SwapsTeamsAndMirrorsEveryOutcome()
    {
        var result = ScoringEngine.Resolve(A(Totals(pts: 500, reb: 100, tov: 40)), B(Totals(pts: 400, reb: 100, tov: 60)));
        var mirrored = result.Invert();

        Assert.Equal(TeamB, mirrored.TeamId);
        Assert.Equal(TeamA, mirrored.OpponentId);
        Assert.Equal(result.Wins, mirrored.Losses);
        Assert.Equal(result.Losses, mirrored.Wins);
        Assert.Equal(result.Ties, mirrored.Ties);
    }

    /// <summary>
    /// Inverting the arguments must give the same answer as inverting the result. If these ever
    /// disagree, a matchup means something different depending on which team is listed first.
    /// </summary>
    [Fact]
    public void Resolve_IsSymmetric()
    {
        var team = Totals(fgm: 5, fga: 12, ftm: 8, fta: 9, fg3m: 7, pts: 480, reb: 210, ast: 95, stl: 44, blk: 25, tov: 51);
        var opponent = Totals(fgm: 6, fga: 11, ftm: 7, fta: 12, fg3m: 7, pts: 505, reb: 190, ast: 110, stl: 44, blk: 18, tov: 49);

        var forward = ScoringEngine.Resolve(A(team), B(opponent));
        var backward = ScoringEngine.Resolve(B(opponent), A(team));

        Assert.Equal(forward.Invert(), backward);
    }

    [Fact]
    public void Resolve_RejectsATeamPlayingItself()
    {
        Assert.Throws<ArgumentException>(() =>
            ScoringEngine.Resolve(A(Totals()), A(Totals())));
    }

    /// <summary>
    /// The property that makes stat corrections survivable: the same lines always produce the
    /// same result, so a matchup can be recomputed rather than patched.
    /// </summary>
    [Fact]
    public void Resolve_IsDeterministic_OverTheSameLines()
    {
        StatLine[] lines = [Line(fgm: 4, fga: 9, pts: 11, reb: 6), Line(fgm: 2, fga: 8, pts: 5, ast: 7)];

        var first = ScoringEngine.Resolve(A(ScoringEngine.Aggregate(lines)), B(CategoryTotals.Empty));
        var second = ScoringEngine.Resolve(A(ScoringEngine.Aggregate(lines)), B(CategoryTotals.Empty));

        Assert.Equal(first, second);
    }
}
