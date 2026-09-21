using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

/// <summary>
/// What survived the deck power score: the four adequacy ratios, and which one comes last. The
/// 0-10 aggregate is gone, so these are about naming a weakness, not grading a deck.
/// </summary>
public class DeckAdviceTests
{
    private static DeckAdviceInputs Balanced(
        double damage = 40, double damageNeeded = 40,
        double block = 20, double incoming = 20,
        int scaling = 3, int scalingNeeded = 3,
        int acceleration = 2, int accelerationNeeded = 2) =>
        new(damage, damageNeeded, block, incoming, scaling, scalingNeeded, acceleration, accelerationNeeded);

    [Fact]
    public void NamesTheCategoryFurthestBehind()
    {
        Assert.Equal("Block", DeckAdvice.Evaluate(Balanced(block: 2)).Weakest);
        Assert.Equal("Damage", DeckAdvice.Evaluate(Balanced(damage: 2)).Weakest);
        Assert.Equal("Scaling", DeckAdvice.Evaluate(Balanced(scaling: 0)).Weakest);
        Assert.Equal("Acceleration", DeckAdvice.Evaluate(Balanced(acceleration: 0)).Weakest);
    }

    [Fact]
    public void ADeckMeetingEveryDemandHasNoWeaknessWorthNaming()
    {
        var result = DeckAdvice.Evaluate(Balanced());

        Assert.True(result.HasData);
        Assert.True(result.Ratio >= DeckAdvice.Adequate);
    }

    [Fact]
    public void CardCountsAloneAreNotEnoughToNameAWeakness()
    {
        // Scaling and acceleration are computable the instant a run exists. On their own they would
        // have the panel naming a weakness before a card had been played.
        var nothingMeasuredYet = Balanced(damage: double.NaN, block: double.NaN, incoming: 0);

        Assert.False(DeckAdvice.Evaluate(nothingMeasuredYet).HasData);
    }

    [Fact]
    public void OneMeasuredCategoryIsEnough()
    {
        var damageOnly = Balanced(block: double.NaN, incoming: 0);

        var result = DeckAdvice.Evaluate(damageOnly);

        Assert.True(result.HasData);
        Assert.NotEqual("Block", result.Weakest);
    }

    [Fact]
    public void AnUnmeasuredCategoryIsNotTreatedAsAZero()
    {
        // NaN means nobody has watched yet; zero means the deck does none of it. Reading the first
        // as the second used to pin the old score to its floor for the whole opening of a run.
        Assert.NotEqual("Damage", DeckAdvice.Evaluate(Balanced(damage: double.NaN)).Weakest);
        Assert.Equal("Damage", DeckAdvice.Evaluate(Balanced(damage: 0)).Weakest);
    }

    [Fact]
    public void BlockIsJudgedAgainstWhatWasThrownNotWhatGotThrough()
    {
        // 20 block a turn against 40 thrown is half of what is needed. Measured against damage that
        // got through, a deck being hit for its full block every turn read as keeping up.
        Assert.Equal("Block", DeckAdvice.Evaluate(Balanced(block: 20, incoming: 40)).Weakest);
        Assert.True(DeckAdvice.Evaluate(Balanced(block: 40, incoming: 40)).Ratio >= DeckAdvice.Adequate);
    }

    [Fact]
    public void SurplusIsCappedSoOneStrengthCannotHideAnother()
    {
        var enormousDamage = DeckAdvice.Evaluate(Balanced(damage: 4000, block: 10, incoming: 20));

        Assert.Equal("Block", enormousDamage.Weakest);
        Assert.Equal(0.5, enormousDamage.Ratio, 3);
    }

    [Fact]
    public void LaterActsDemandMoreScalingAndAcceleration()
    {
        Assert.True(DeckAdvice.ScalingNeededForAct(3) > DeckAdvice.ScalingNeededForAct(1));
        Assert.True(DeckAdvice.AccelerationNeededForAct(3) > DeckAdvice.AccelerationNeededForAct(1));
    }

    [Fact]
    public void DamageNeedIsDerivedFromEliteHealth()
    {
        Assert.Equal(50, DeckAdvice.DamageNeededForElite(250), 3);
        Assert.True(double.IsNaN(DeckAdvice.DamageNeededForElite(0)));
    }
}
