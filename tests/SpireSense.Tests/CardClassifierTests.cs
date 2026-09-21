using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

public class CardClassifierTests
{
    public CardClassifierTests() => TestData.LoadRealTables();

    [Fact]
    public void UsesTheCuratedTableWhenTheCardIsKnown()
    {
        var result = CardClassifier.Classify(CardFacts.Named("DemonForm", CardKind.Power));

        Assert.Equal(ClassificationSource.Curated, result.Source);
        Assert.Contains(Category.ScalingDamage, result.Categories);
    }

    [Fact]
    public void CuratedVerdictBeatsWhatTheCardDataSuggests()
    {
        // Body Slam is an attack with no base damage; the table says Scaling, and that must win
        // over any heuristic reading of its card data.
        var bodySlam = TestData.Attack("BodySlam", damage: 99);

        var result = CardClassifier.Classify(bodySlam);

        Assert.Equal(ClassificationSource.Curated, result.Source);
        Assert.DoesNotContain(Category.FrontloadedDamage, result.Categories);
    }

    [Theory]
    [InlineData(CardKind.Curse)]
    [InlineData(CardKind.Status)]
    public void CursesAndStatusesNeverCountTowardACategory(CardKind kind)
    {
        var result = CardClassifier.Classify(CardFacts.Named(TestData.UnknownCardName, kind));

        Assert.Equal(ClassificationSource.Ignored, result.Source);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public void IgnoresCursesEvenWhenTheyAppearInTheTables()
    {
        // The ignore check runs before the table lookup, so a curse can never be miscounted.
        var result = CardClassifier.Classify(CardFacts.Named("DemonForm", CardKind.Curse));

        Assert.Equal(ClassificationSource.Ignored, result.Source);
    }

    [Fact]
    public void GuessesScalingForAnUnknownPower()
    {
        var result = CardClassifier.Classify(TestData.Power(TestData.UnknownCardName));

        Assert.Equal(ClassificationSource.Heuristic, result.Source);
        Assert.Equal(new[] { Category.ScalingDamage }, result.Categories);
    }

    [Fact]
    public void GuessesDamageAndAoeForAnUnknownSweepingAttack()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 8, allEnemies: true));

        Assert.Equal(ClassificationSource.Heuristic, result.Source);
        Assert.Contains(Category.FrontloadedDamage, result.Categories);
        Assert.Contains(Category.Aoe, result.Categories);
    }

    [Fact]
    public void DoesNotGuessAoeForASingleTargetAttack()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 8));

        Assert.Contains(Category.FrontloadedDamage, result.Categories);
        Assert.DoesNotContain(Category.Aoe, result.Categories);
    }

    [Fact]
    public void DoesNotGuessDamageForAnAttackWithNoBaseDamage()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 0));

        Assert.Empty(result.Categories);
    }

    [Fact]
    public void GuessesBlockAndDrawTogether()
    {
        var result = CardClassifier.Classify(TestData.Skill(TestData.UnknownCardName, block: 5, draw: 2));

        Assert.Contains(Category.FrontloadedBlock, result.Categories);
        Assert.Contains(Category.Acceleration, result.Categories);
    }

    [Fact]
    public void GuessesBlockFromTheGainsBlockFlagAlone()
    {
        // Some cards gain block through an effect rather than a Block value.
        var card = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { GainsBlock = true };

        Assert.Contains(Category.FrontloadedBlock, CardClassifier.Classify(card).Categories);
    }

    [Fact]
    public void CountsSelfStrengthAsScalingButNotStrengthAppliedToEnemies()
    {
        var onSelf = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { TargetsSelf = true, StrengthGain = 2 };
        var onEnemy = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { TargetsSelf = false, StrengthGain = 2 };

        Assert.Contains(Category.ScalingDamage, CardClassifier.Classify(onSelf).Categories);
        Assert.DoesNotContain(Category.ScalingDamage, CardClassifier.Classify(onEnemy).Categories);
    }
}
