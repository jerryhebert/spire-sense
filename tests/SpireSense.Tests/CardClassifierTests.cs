using SpireSense.SpireSenseCode.Jobs;
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
        Assert.Contains(Job.Scaling, result.Jobs);
    }

    [Fact]
    public void CuratedVerdictBeatsWhatTheCardDataSuggests()
    {
        // Body Slam is an attack with no base damage; the table says Scaling, and that must win
        // over any heuristic reading of its card data.
        var bodySlam = TestData.Attack("BodySlam", damage: 99);

        var result = CardClassifier.Classify(bodySlam);

        Assert.Equal(ClassificationSource.Curated, result.Source);
        Assert.DoesNotContain(Job.FrontloadedDamage, result.Jobs);
    }

    [Theory]
    [InlineData(CardKind.Curse)]
    [InlineData(CardKind.Status)]
    public void CursesAndStatusesNeverCountTowardAJob(CardKind kind)
    {
        var result = CardClassifier.Classify(CardFacts.Named(TestData.UnknownCardName, kind));

        Assert.Equal(ClassificationSource.Ignored, result.Source);
        Assert.Empty(result.Jobs);
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
        Assert.Equal(new[] { Job.Scaling }, result.Jobs);
    }

    [Fact]
    public void GuessesDamageAndAoeForAnUnknownSweepingAttack()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 8, allEnemies: true));

        Assert.Equal(ClassificationSource.Heuristic, result.Source);
        Assert.Contains(Job.FrontloadedDamage, result.Jobs);
        Assert.Contains(Job.FrontloadedAoe, result.Jobs);
    }

    [Fact]
    public void DoesNotGuessAoeForASingleTargetAttack()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 8));

        Assert.Contains(Job.FrontloadedDamage, result.Jobs);
        Assert.DoesNotContain(Job.FrontloadedAoe, result.Jobs);
    }

    [Fact]
    public void DoesNotGuessDamageForAnAttackWithNoBaseDamage()
    {
        var result = CardClassifier.Classify(TestData.Attack(TestData.UnknownCardName, 0));

        Assert.Empty(result.Jobs);
    }

    [Fact]
    public void GuessesBlockAndDrawTogether()
    {
        var result = CardClassifier.Classify(TestData.Skill(TestData.UnknownCardName, block: 5, draw: 2));

        Assert.Contains(Job.FrontloadedBlock, result.Jobs);
        Assert.Contains(Job.CardDraw, result.Jobs);
    }

    [Fact]
    public void GuessesBlockFromTheGainsBlockFlagAlone()
    {
        // Some cards gain block through an effect rather than a Block value.
        var card = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { GainsBlock = true };

        Assert.Contains(Job.FrontloadedBlock, CardClassifier.Classify(card).Jobs);
    }

    [Fact]
    public void CountsSelfStrengthAsScalingButNotStrengthAppliedToEnemies()
    {
        var onSelf = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { TargetsSelf = true, StrengthGain = 2 };
        var onEnemy = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { TargetsSelf = false, StrengthGain = 2 };

        Assert.Contains(Job.Scaling, CardClassifier.Classify(onSelf).Jobs);
        Assert.DoesNotContain(Job.Scaling, CardClassifier.Classify(onEnemy).Jobs);
    }
}
