using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

/// <summary>
/// Regression guards for four defects found by reading the score back against a screenshot of it.
/// Each one made the number say something the run had not earned.
/// </summary>
public class DeckPowerDefectTests
{
    public DeckPowerDefectTests() => TestData.LoadRealTables();

    // ------------------------------------------------------------------ A1

    [Fact]
    public void CardCountsAloneAreNotEnoughToReportAScore()
    {
        // Scaling and acceleration are card counts against per-act judgement calls, computable the
        // instant a run exists. They used to satisfy the "is there data" check by themselves, so
        // the panel showed a confident score before a card had been played — and because unmeasured
        // damage arrived as a plain zero, that score was the bottom of the scale.
        var nothingMeasuredYet = new DeckPowerInputs(
            DamagePerTurn: double.NaN, DamageNeededPerTurn: 38,
            MitigationPerTurn: double.NaN, IncomingDamagePerTurn: 0,
            ScalingCards: 2, ScalingNeeded: 2,
            AccelerationCards: 1, AccelerationNeeded: 1);

        Assert.False(DeckPower.Evaluate(nothingMeasuredYet).HasData);
    }

    [Fact]
    public void OneMeasuredCategoryIsEnoughToReportAScore()
    {
        var damageOnly = new DeckPowerInputs(
            DamagePerTurn: 38, DamageNeededPerTurn: 38,
            MitigationPerTurn: double.NaN, IncomingDamagePerTurn: 0,
            ScalingCards: 2, ScalingNeeded: 2,
            AccelerationCards: 1, AccelerationNeeded: 1);

        var result = DeckPower.Evaluate(damageOnly);

        Assert.True(result.HasData);
        Assert.NotEqual("Block", result.LimitedBy);
    }

    [Fact]
    public void AnUnmeasuredCategoryIsNotScoredAsAZero()
    {
        // NaN means nobody has watched yet; zero means the deck does none of it. Reading the first
        // as the second is what pinned the opening score to the floor.
        var unmeasured = Inputs(damage: double.NaN);
        var genuinelyZero = Inputs(damage: 0);

        Assert.True(DeckPower.Evaluate(unmeasured).Score > DeckPower.Evaluate(genuinelyZero).Score);
    }

    // ------------------------------------------------------------------ A2

    [Fact]
    public void BlockIsJudgedAgainstWhatWasThrownNotWhatGotThrough()
    {
        // 20 block a turn against 40 damage a turn is half of what is needed, however the timing
        // works out. Measured against damage that got through, the same deck read as adequate:
        // block 20, leaked 20, ratio 1.0 — a deck being hit for its full block every turn scoring
        // as if it were keeping up.
        var half = DeckPower.Evaluate(Inputs(block: 20, incoming: 40));
        var keepingUp = DeckPower.Evaluate(Inputs(block: 40, incoming: 40));

        Assert.Equal("Block", half.LimitedBy);
        Assert.True(half.Score < keepingUp.Score);
    }

    [Fact]
    public void BlockingEverythingDoesNotRemoveBlockFromTheScore()
    {
        // The old denominator was damage that landed, so a deck blocking everything reported no
        // incoming damage, the ratio went NaN, and the category dropped out of the average
        // entirely — the score stopped counting block exactly when block was working.
        var flawless = DeckPower.Evaluate(Inputs(block: 60, incoming: 40));

        Assert.True(flawless.HasData);
        Assert.NotEqual("Block", flawless.LimitedBy);
        Assert.True(flawless.Score >= DeckPower.Evaluate(Inputs(block: 40, incoming: 40)).Score);
    }

    [Fact]
    public void RunStatsCountsIncomingDamageAcrossTurns()
    {
        var stats = new RunStats();
        stats.NoteTurn("combat:1");
        stats.AddIncomingDamage(30);
        stats.NoteTurn("combat:2");
        stats.AddIncomingDamage(10);

        Assert.Equal(40, stats.TotalIncomingDamage);
        Assert.Equal(20, stats.IncomingDamagePerTurn);
    }

    // ------------------------------------------------------------------ A3

    [Fact]
    public void ACardThatScalesBothWaysCountsOnceTowardScaling()
    {
        // Resonance is ScalingDamage and ScalingBlock. Summing the two category counts made it
        // worth two cards against a requirement of two to four, so a single card could fill half
        // the act-one requirement on its own.
        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("Resonance", CardKind.Skill) });

        Assert.Equal(1, analysis.Counts[Category.ScalingDamage]);
        Assert.Equal(1, analysis.Counts[Category.ScalingBlock]);
        Assert.Equal(1, analysis.ScalingCards);
    }

    [Fact]
    public void CardsScalingOnlyOneWayStillCountTowardScaling()
    {
        var analysis = DeckAnalysis.Analyze(new[]
        {
            CardFacts.Named("Resonance", CardKind.Skill),      // both
            CardFacts.Named("NoxiousFumes", CardKind.Power),   // damage only
            CardFacts.Named("StrikeIronclad", CardKind.Attack),// neither
        });

        Assert.Equal(2, analysis.ScalingCards);
    }

    private static DeckPowerInputs Inputs(
        double damage = 40, double damageNeeded = 40,
        double block = 20, double incoming = 20,
        int scaling = 3, int scalingNeeded = 3,
        int acceleration = 2, int accelerationNeeded = 2) =>
        new(damage, damageNeeded, block, incoming, scaling, scalingNeeded, acceleration, accelerationNeeded);
}
