using SpireSense.SpireSenseCode.Jobs;
using Xunit;

namespace SpireSense.Tests;

public class CycleEstimateTests
{
    public CycleEstimateTests() => TestData.LoadRealTables();

    private static CardFacts Attack(decimal damage, int cost) =>
        CardFacts.Named("ZzzAttack", CardKind.Attack) with { Damage = damage, EnergyCost = cost };

    private static CardFacts Defend(decimal block, int cost) =>
        CardFacts.Named("ZzzDefend", CardKind.Skill) with { Block = block, EnergyCost = cost };

    [Theory]
    [InlineData(15, 5, 3.0)]
    [InlineData(15, 7, 15.0 / 7)]
    [InlineData(10, 5, 2.0)]
    [InlineData(12, 5, 2.4)]
    [InlineData(0, 5, 0.0)]
    public void ACycleLastsDeckSizeDividedByDraw(int deckSize, double draw, double expected)
    {
        // Fractional on purpose: 15 cards at 7 draw is 15/7 turns, not 3. Rounding up would
        // overstate every deck whose size is not a multiple of its draw.
        Assert.Equal(expected, CycleEstimate.TurnsPerCycle(deckSize, draw), 4);
    }

    [Fact]
    public void AnUnknownDrawFallsBackToTheBaseFive()
    {
        Assert.Equal(3.0, CycleEstimate.TurnsPerCycle(15, 0), 4);
    }

    [Fact]
    public void AMeasuredRateIsMultipliedOutToAWholeCycle()
    {
        // 36 damage a turn with a 15 card deck at 5 draw is 3 turns, so 108 over a cycle.
        Assert.Equal(108, CycleEstimate.FromPerTurn(36, deckSize: 15, cardsDrawnPerTurn: 5), 3);
    }

    [Fact]
    public void MoreDrawShortensTheCycleAndSoLowersTheCycleTotal()
    {
        var atFive = CycleEstimate.FromPerTurn(36, deckSize: 15, cardsDrawnPerTurn: 5);
        var atSeven = CycleEstimate.FromPerTurn(36, deckSize: 15, cardsDrawnPerTurn: 7);

        Assert.True(atSeven < atFive);
        Assert.Equal(36 * (15.0 / 7), atSeven, 3);
    }

    [Fact]
    public void ACheapDeckIsPlayedInFull()
    {
        // 10 cards is 2 turns, so 6 energy. A deck costing 4 fits comfortably.
        Assert.Equal(1.0, CycleEstimate.PlayableFraction(deckSize: 10, totalEnergyCost: 4), 3);
    }

    [Fact]
    public void AnExpensiveDeckIsThrottledByEnergy()
    {
        // 10 cards is 2 turns, so 6 energy against a deck costing 12: half of it gets played.
        Assert.Equal(0.5, CycleEstimate.PlayableFraction(deckSize: 10, totalEnergyCost: 12), 3);
    }

    [Fact]
    public void AFreeDeckIsNeverThrottled()
    {
        Assert.Equal(1.0, CycleEstimate.PlayableFraction(deckSize: 10, totalEnergyCost: 0), 3);
    }

    [Fact]
    public void AnEmptyDeckEstimatesNothingRatherThanDividingByZero()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        Assert.Equal(0, analysis.AvgCycleDamage, 3);
        Assert.Equal(0, analysis.AvgCycleMitigation, 3);
    }

    [Fact]
    public void TheEstimateIsTheWholeDeckThrottledByEnergy()
    {
        // 10 attacks of 6 for 1 energy: 60 damage over a 2 turn cycle, but 6 energy covers a cost
        // of 10 only 60% of the way, so 36 lands in one cycle.
        var deck = Enumerable.Repeat(Attack(6, 1), 10).ToList();

        Assert.Equal(36.0, DeckAnalysis.Analyze(deck).AvgCycleDamage, 2);
    }

    [Fact]
    public void BlockUsesTheSameModelAsDamage()
    {
        var deck = Enumerable.Repeat(Defend(6, 1), 10).ToList();

        Assert.Equal(36.0, DeckAnalysis.Analyze(deck).AvgCycleMitigation, 2);
    }

    [Fact]
    public void CursesLengthenTheCycleRatherThanReducingItsTotal()
    {
        // A consequence of measuring whole cycles rather than turns, and worth pinning so nobody
        // "fixes" it later: over one full pass you still draw and play every real card, so the
        // damage in a cycle does not fall. What curses cost you is time. They make the cycle take
        // more turns, which is exactly what a per-turn figure would have shown instead.
        var lean = Enumerable.Repeat(Attack(6, 1), 10).ToList();
        var bloated = lean.Concat(Enumerable.Repeat(TestData.Curse("Regret"), 10)).ToList();

        var leanAnalysis = DeckAnalysis.Analyze(lean);
        var bloatedAnalysis = DeckAnalysis.Analyze(bloated);

        Assert.True(bloatedAnalysis.AvgCycleDamage >= leanAnalysis.AvgCycleDamage);
        Assert.True(CycleEstimate.TurnsPerCycle(bloatedAnalysis.TotalCards)
            > CycleEstimate.TurnsPerCycle(leanAnalysis.TotalCards));
    }

    [Fact]
    public void CursesCostNoEnergyBecauseTheyCannotBePlayed()
    {
        // A curse must not make the deck look expensive; it makes it look long, which it is.
        var withCurses = Enumerable.Repeat(Attack(6, 1), 10)
            .Concat(Enumerable.Repeat(TestData.Curse("Regret") with { EnergyCost = 3 }, 5))
            .ToList();

        // 15 cards is 3 turns and 9 energy against a real cost of 10, so 90% of 60 lands.
        Assert.Equal(54.0, DeckAnalysis.Analyze(withCurses).AvgCycleDamage, 1);
    }

    [Fact]
    public void AnXCostCardIsBudgetedAsAFullTurnOfEnergy()
    {
        // Costing X at zero would make a deck full of them look free.
        var xCost = CardFacts.Named("ZzzX", CardKind.Attack) with { Damage = 10, CostsX = true };
        var free = CardFacts.Named("ZzzFree", CardKind.Attack) with { Damage = 10, EnergyCost = 0 };

        var withX = DeckAnalysis.Analyze(Enumerable.Repeat(xCost, 10).ToList()).AvgCycleDamage;
        var withFree = DeckAnalysis.Analyze(Enumerable.Repeat(free, 10).ToList()).AvgCycleDamage;

        Assert.True(withX < withFree);
    }

    [Fact]
    public void ChangingTheEstimateMakesTheOverlayRedraw()
    {
        var lean = Enumerable.Repeat(Attack(6, 1), 10).ToList();
        var stronger = Enumerable.Repeat(Attack(12, 1), 10).ToList();

        Assert.NotEqual(DeckAnalysis.Analyze(lean), DeckAnalysis.Analyze(stronger));
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(18.0, "18")]
    [InlineData(7.25, "7")]
    [InlineData(7.5, "8")]
    public void FiguresShowAsWholeNumbers(double value, string expected)
    {
        // These are rough figures; a decimal place would imply precision they do not have.
        Assert.Equal(expected, CycleFigures.Format(value));
    }
}
