using SpireSense.SpireSenseCode.Categories;
using SpireSense.SpireSenseCode.Overlay;
using Xunit;

namespace SpireSense.Tests;

public class DeckPowerTests
{
    public DeckPowerTests() => TestData.LoadRealTables();

    private static DeckPowerInputs Balanced(
        double damage = 40, double damageNeeded = 40,
        double block = 20, double taken = 20,
        int scaling = 3, int scalingNeeded = 3,
        int acceleration = 2, int accelerationNeeded = 2) =>
        new(damage, damageNeeded, block, taken, scaling, scalingNeeded, acceleration, accelerationNeeded);

    [Fact]
    public void ADeckMeetingEveryDemandScoresHighly()
    {
        // Exactly meeting every demand is 7 of 10: comfortable, with the top of the scale held
        // back for a deck that is genuinely ahead of what the act asks of it.
        Assert.Equal(7.0, DeckPower.Evaluate(Balanced()).Score, 1);
    }

    [Fact]
    public void AMissingCategoryCollapsesTheScore()
    {
        // The whole reason for a geometric mean rather than a sum. No block at all is fatal no
        // matter how good the rest of the deck looks.
        var noBlock = DeckPower.Evaluate(Balanced(block: 0));

        Assert.True(noBlock.Score < 2.5, $"expected a collapsed score, got {noBlock.Score}");
        Assert.Equal("Block", noBlock.LimitedBy);
    }

    [Fact]
    public void SurplusInOneCategoryCannotBuyOffAGapInAnother()
    {
        // Twice the damage you need does not make up for half the block you need, because in a
        // fight it does not.
        var lopsided = DeckPower.Evaluate(Balanced(damage: 400, block: 10));
        var balanced = DeckPower.Evaluate(Balanced());

        Assert.True(lopsided.Score < balanced.Score);
        Assert.Equal("Block", lopsided.LimitedBy);
    }

    [Fact]
    public void TheWeakestCategoryIsTheOneNamed()
    {
        Assert.Equal("Acceleration", DeckPower.Evaluate(Balanced(acceleration: 0)).LimitedBy);
        Assert.Equal("Scaling", DeckPower.Evaluate(Balanced(scaling: 0)).LimitedBy);
        Assert.Equal("Damage", DeckPower.Evaluate(Balanced(damage: 2)).LimitedBy);
    }

    [Fact]
    public void NoDemandsKnownYetMeansNoScore()
    {
        // Before the first fight there is nothing to compare against, and a confident number there
        // would be worse than none.
        var nothing = new DeckPowerInputs(0, double.NaN, 0, 0, 0, 0, 0, 0);

        Assert.False(DeckPower.Evaluate(nothing).HasData);
    }

    [Fact]
    public void ACategoryWithNoKnownDemandIsSkippedRatherThanCountedAsFailure()
    {
        // Damage demand is known, block demand is not; the score should reflect damage alone rather
        // than treating the unknown as a zero.
        var partial = new DeckPowerInputs(40, 40, 0, 0, 3, 3, 2, 2);

        var result = DeckPower.Evaluate(partial);

        Assert.True(result.HasData);
        Assert.NotEqual("Block", result.LimitedBy);
    }

    [Fact]
    public void DamageNeedIsDerivedFromEliteHealth()
    {
        // A 250 HP elite in 5 turns is 50 a turn.
        Assert.Equal(50, DeckPower.DamageNeededForElite(250), 3);
        Assert.True(double.IsNaN(DeckPower.DamageNeededForElite(0)));
    }

    [Fact]
    public void LaterActsDemandMoreScalingAndAcceleration()
    {
        Assert.True(DeckPower.ScalingNeededForAct(3) > DeckPower.ScalingNeededForAct(1));
        Assert.True(DeckPower.AccelerationNeededForAct(3) > DeckPower.AccelerationNeededForAct(1));
    }

    [Fact]
    public void TheSameDeckScoresLowerLaterInTheRun()
    {
        // Act 3 asks for more of the same deck, which is the point of scaling the demands.
        var act1 = DeckPower.Evaluate(Balanced(scalingNeeded: DeckPower.ScalingNeededForAct(1), accelerationNeeded: DeckPower.AccelerationNeededForAct(1)));
        var act3 = DeckPower.Evaluate(Balanced(scalingNeeded: DeckPower.ScalingNeededForAct(3), accelerationNeeded: DeckPower.AccelerationNeededForAct(3)));

        Assert.True(act3.Score < act1.Score);
    }

    [Fact]
    public void TheScoreAndTheLimitingCategoryBothReachThePanel()
    {
        // Regression guard: an earlier change added a render method and never called it.
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());
        var power = new DeckPowerResult(6.2, "Block", HasData: true);

        var text = OverlayText.Build(analysis, true, new CycleFigures(10, 5, 3, 5.0, Measured: true), power).ToString();

        // The figure is wrapped in a font tag, so the label and the number are matched separately.
        Assert.Contains("Power ", text);
        Assert.Contains("6.2", text);
        Assert.Contains("held back by Block", text);
    }

    [Fact]
    public void TheScoreStaysInsideTheOneToTenScale()
    {
        // The unbounded-looking number it replaced gave nothing to judge a deck against. Whatever
        // the inputs, the answer has to land somewhere a player can read at a glance.
        var hopeless = DeckPower.Evaluate(Balanced(damage: 0, block: 0, scaling: 0, acceleration: 0));
        var overwhelming = DeckPower.Evaluate(Balanced(damage: 4000, block: 2000, scaling: 40, acceleration: 40));

        Assert.InRange(hopeless.Score, DeckPower.MinScore, DeckPower.MaxScore);
        Assert.InRange(overwhelming.Score, DeckPower.MinScore, DeckPower.MaxScore);
        Assert.Equal(DeckPower.MaxScore, overwhelming.Score, 1);
    }

    [Fact]
    public void SurplusBeyondTheCapStopsRaisingTheScore()
    {
        // Ten means "everything this act asks for, with margin", not "unbeatable". Past the cap
        // more of the same buys nothing, so the top of the scale stays reachable and meaningful.
        var ample = DeckPower.Evaluate(Balanced(damage: 60, block: 30, scaling: 5, acceleration: 3));
        var absurd = DeckPower.Evaluate(Balanced(damage: 600, block: 300, scaling: 50, acceleration: 30));

        Assert.Equal(ample.Score, absurd.Score, 1);
    }

    [Fact]
    public void TheScaleIsShownToOneDecimalPlace()
    {
        Assert.Equal("6.2", new DeckPowerResult(6.2, "Block", HasData: true).Label);
        Assert.Equal("7.0", new DeckPowerResult(7.0, "Block", HasData: true).Label);
    }

    [Fact]
    public void BeforeThereIsDataThePanelSaysSoRatherThanShowingZero()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        var text = OverlayText.Build(analysis, true, new CycleFigures(0, 0, 0, 5.0, Measured: false), default).ToString();

        Assert.Contains("measuring", text);
        Assert.DoesNotContain("Power 0", text);
    }
}
