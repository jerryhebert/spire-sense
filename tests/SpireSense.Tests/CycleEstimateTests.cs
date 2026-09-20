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
    [InlineData(10, 2)]
    [InlineData(5, 1)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(11, 3)]
    public void ACycleLastsAsLongAsItTakesToDrawTheDeck(int deckSize, int expectedTurns)
    {
        Assert.Equal(expectedTurns, CycleEstimate.TurnsPerCycle(deckSize));
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
    public void DamageIsAveragedOverTheTurnsItTakesToDrawTheDeck()
    {
        // 10 attacks of 6 for 1 energy: 60 damage, 2 turns, and 6 energy covers a cost of 10 only
        // 60% of the way, so 60 * 0.6 / 2 = 18 per turn.
        var deck = Enumerable.Repeat(Attack(6, 1), 10).ToList();

        Assert.Equal(18.0, DeckAnalysis.Analyze(deck).AvgCycleDamage, 2);
    }

    [Fact]
    public void BlockUsesTheSameModelAsDamage()
    {
        var deck = Enumerable.Repeat(Defend(6, 1), 10).ToList();

        Assert.Equal(18.0, DeckAnalysis.Analyze(deck).AvgCycleMitigation, 2);
    }

    [Fact]
    public void AddingCursesLowersTheEstimate()
    {
        // The point of averaging over a cycle: dead cards you must draw through really do cost you
        // damage per turn, and a model that ignored deck size would miss that entirely.
        var lean = Enumerable.Repeat(Attack(6, 1), 10).ToList();
        var bloated = lean.Concat(Enumerable.Repeat(TestData.Curse("Regret"), 10)).ToList();

        Assert.True(DeckAnalysis.Analyze(bloated).AvgCycleDamage < DeckAnalysis.Analyze(lean).AvgCycleDamage);
    }

    [Fact]
    public void CursesCostNoEnergyBecauseTheyCannotBePlayed()
    {
        // A curse must not make the deck look expensive; it makes it look long, which it is.
        var withCurses = Enumerable.Repeat(Attack(6, 1), 10)
            .Concat(Enumerable.Repeat(TestData.Curse("Regret") with { EnergyCost = 3 }, 5))
            .ToList();

        // 15 cards is 3 turns and 9 energy against a real cost of 10, so nearly all of it plays:
        // 60 * 0.9 / 3 = 18.
        Assert.Equal(18.0, DeckAnalysis.Analyze(withCurses).AvgCycleDamage, 1);
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
