using SpireSense.SpireSenseCode.Jobs;
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
        int draw = 2, int drawNeeded = 2) =>
        new(damage, damageNeeded, block, taken, scaling, scalingNeeded, draw, drawNeeded);

    [Fact]
    public void ADeckMeetingEveryDemandScoresHighly()
    {
        Assert.True(DeckPower.Evaluate(Balanced()).Score >= 65);
    }

    [Fact]
    public void AMissingJobCollapsesTheScore()
    {
        // The whole reason for a geometric mean rather than a sum. No block at all is fatal no
        // matter how good the rest of the deck looks.
        var noBlock = DeckPower.Evaluate(Balanced(block: 0));

        Assert.True(noBlock.Score < 20);
        Assert.Equal("Block", noBlock.LimitedBy);
    }

    [Fact]
    public void SurplusInOneJobCannotBuyOffAGapInAnother()
    {
        // Twice the damage you need does not make up for half the block you need, because in a
        // fight it does not.
        var lopsided = DeckPower.Evaluate(Balanced(damage: 400, block: 10));
        var balanced = DeckPower.Evaluate(Balanced());

        Assert.True(lopsided.Score < balanced.Score);
        Assert.Equal("Block", lopsided.LimitedBy);
    }

    [Fact]
    public void TheWeakestJobIsTheOneNamed()
    {
        Assert.Equal("Draw", DeckPower.Evaluate(Balanced(draw: 0)).LimitedBy);
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
    public void AJobWithNoKnownDemandIsSkippedRatherThanCountedAsFailure()
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
    public void LaterActsDemandMoreScalingAndDraw()
    {
        Assert.True(DeckPower.ScalingNeededForAct(3) > DeckPower.ScalingNeededForAct(1));
        Assert.True(DeckPower.DrawNeededForAct(3) > DeckPower.DrawNeededForAct(1));
    }

    [Fact]
    public void TheSameDeckScoresLowerLaterInTheRun()
    {
        // Act 3 asks for more of the same deck, which is the point of scaling the demands.
        var act1 = DeckPower.Evaluate(Balanced(scalingNeeded: DeckPower.ScalingNeededForAct(1), drawNeeded: DeckPower.DrawNeededForAct(1)));
        var act3 = DeckPower.Evaluate(Balanced(scalingNeeded: DeckPower.ScalingNeededForAct(3), drawNeeded: DeckPower.DrawNeededForAct(3)));

        Assert.True(act3.Score < act1.Score);
    }

    [Fact]
    public void TheScoreAndTheLimitingJobBothReachThePanel()
    {
        // Regression guard: an earlier change added a render method and never called it.
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());
        var power = new DeckPowerResult(62, "Block", HasData: true);

        var text = OverlayText.Build(analysis, true, new CycleFigures(10, 5, 3, 5.0, Measured: true), power);

        Assert.Contains("Power 62", text);
        Assert.Contains("held back by Block", text);
    }

    [Fact]
    public void BeforeThereIsDataThePanelSaysSoRatherThanShowingZero()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        var text = OverlayText.Build(analysis, true, new CycleFigures(0, 0, 0, 5.0, Measured: false), default);

        Assert.Contains("measuring", text);
        Assert.DoesNotContain("Power 0", text);
    }
}
