using SpireSense.SpireSenseCode.Categories;
using SpireSense.SpireSenseCode.Overlay;
using Xunit;

namespace SpireSense.Tests;

public class RunStatsTests
{
    public RunStatsTests() => TestData.LoadRealTables();

    [Fact]
    public void AverageIsTotalOverTurnsPlayed()
    {
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddDamage(30);
        stats.NoteTurn("c1:2");
        stats.AddDamage(10);

        Assert.Equal(2, stats.TurnsObserved);
        Assert.Equal(20, stats.DamagePerTurn, 3);
    }

    [Fact]
    public void RepeatedTicksWithinOneTurnCountAsOneTurn()
    {
        // The overlay registers the current turn every frame, so the same key arrives many times.
        var stats = new RunStats();
        for (var i = 0; i < 50; i++) stats.NoteTurn("c1:1");

        Assert.Equal(1, stats.TurnsObserved);
    }

    [Fact]
    public void AQuietTurnStillCountsAndPullsTheAverageDown()
    {
        // A turn where you neither attacked nor blocked is real. Dropping it would flatter the
        // figures, which is the opposite of what they are for.
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddDamage(30);
        stats.NoteTurn("c1:2");

        Assert.Equal(15, stats.DamagePerTurn, 3);
    }

    [Fact]
    public void ANewCombatStartsItsTurnNumbersAgainWithoutLosingCount()
    {
        // Turn 1 of combat two must not be mistaken for turn 1 of combat one.
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.NoteTurn("c1:2");
        stats.NoteTurn("c2:1");

        Assert.Equal(3, stats.TurnsObserved);
    }

    [Fact]
    public void BlockUsesTheSameAveraging()
    {
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddMitigation(12);
        stats.NoteTurn("c1:2");
        stats.AddMitigation(8);

        Assert.Equal(10, stats.MitigationPerTurn, 3);
    }

    [Fact]
    public void NegativeAndZeroAmountsAreIgnored()
    {
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddDamage(-5);
        stats.AddMitigation(0);

        Assert.Equal(0, stats.TotalDamage, 3);
        Assert.Equal(0, stats.TotalMitigation, 3);
    }

    [Fact]
    public void BeforeAnyTurnThereIsNothingToReport()
    {
        var stats = new RunStats();

        Assert.False(stats.HasData);
        Assert.Equal(0, stats.DamagePerTurn, 3);
    }

    [Fact]
    public void DrawIsAveragedOverTurnsLikeTheOtherFigures()
    {
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddCardsDrawn(5);
        stats.AddCardsDrawn(2);
        stats.NoteTurn("c1:2");
        stats.AddCardsDrawn(5);

        // Seven cards on the first turn, five on the second.
        Assert.Equal(6, stats.CardsDrawnPerTurn, 3);
    }

    [Fact]
    public void DrawCountsEffectsNotJustTheOpeningHand()
    {
        // The point of measuring rather than reading the hand size: a draw engine shows up here
        // and does not show up in the size of the hand you are dealt.
        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.NoteHandDraw(5);
        stats.AddCardsDrawn(5);
        stats.AddCardsDrawn(4);

        Assert.Equal(5, stats.HandDrawSize, 3);
        Assert.Equal(9, stats.CardsDrawnPerTurn, 3);
    }

    [Fact]
    public void DrawReachesThePanel()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        var text = OverlayText.Build(analysis, true, new CycleFigures(36, 23, 4.0, 7.5, Measured: true), default, default, new RunStats()).ToString();

        Assert.Contains("Draw:", text);
        Assert.Contains("7.5", text);
    }

    [Fact]
    public void StartingANewRunClearsTheFigures()
    {
        RunStats.Current.NoteTurn("c1:1");
        RunStats.Current.AddDamage(100);

        RunStats.StartNewRun();

        Assert.False(RunStats.Current.HasData);
        Assert.Equal(0, RunStats.Current.TotalDamage, 3);
    }

    [Fact]
    public void TheEstimateStandsInUntilATurnHasBeenPlayed()
    {
        var deck = Enumerable.Repeat(
            CardFacts.Named("Zzz", CardKind.Attack) with { Damage = 6, EnergyCost = 1 }, 10).ToList();
        var analysis = DeckAnalysis.Analyze(deck);

        var figures = CycleFigures.From(analysis, new RunStats());

        // The analysis holds a whole-cycle total; the displayed figure divides it by cycle length,
        // which for 10 cards at the base 5 draw is 2 turns.
        Assert.False(figures.Measured);
        Assert.Equal(analysis.AvgCycleDamage / 2, figures.Damage, 3);
        Assert.Equal(2.0, figures.CycleTurns, 3);
    }

    [Fact]
    public void MeasuredFiguresReplaceTheEstimateOnceThereIsData()
    {
        var deck = Enumerable.Repeat(
            CardFacts.Named("Zzz", CardKind.Attack) with { Damage = 6, EnergyCost = 1 }, 10).ToList();
        var analysis = DeckAnalysis.Analyze(deck);

        var stats = new RunStats();
        stats.NoteTurn("c1:1");
        stats.AddDamage(41);
        stats.AddMitigation(23);

        var figures = CycleFigures.From(analysis, stats);

        // Measured rates are already per turn, so they pass through unchanged.
        Assert.True(figures.Measured);
        Assert.Equal(41, figures.Damage, 3);
        Assert.Equal(23, figures.Mitigation, 3);
        Assert.Equal(2.0, figures.CycleTurns, 3);
    }

    [Fact]
    public void TheSectionIsLabelledPerTurn()
    {
        // The figures are a per-turn rate. A cycle total was a moving target, since the cycle
        // lengthens as the deck grows, so the same number meant different things in Act 1 and Act 3.
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        var text = OverlayText.Build(analysis, true, new CycleFigures(36, 23, 4.0, 5.0, Measured: true), default, default, new RunStats()).ToString();

        Assert.Contains("Per turn", text);
        Assert.DoesNotContain("turns", text.Replace("Per turn", ""));
    }

    [Fact]
    public void TheEstimateIsLabelledAndTheMeasurementIsNot()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        var estimated = OverlayText.Build(analysis, true, new CycleFigures(10, 5, 3.0, 5.0, Measured: false), default, default, new RunStats()).ToString();
        var measured = OverlayText.Build(analysis, true, new CycleFigures(10, 5, 3.0, 5.0, Measured: true), default, default, new RunStats()).ToString();

        Assert.Contains("estimated", estimated);
        Assert.DoesNotContain("estimated", measured);
        Assert.Contains("Per turn", measured);
        Assert.Contains("Damage:", measured);
        Assert.Contains("Mitigation:", measured);
    }
}
