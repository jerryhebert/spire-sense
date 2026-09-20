using SpireSense.SpireSenseCode.Jobs;
using Xunit;

namespace SpireSense.Tests;

public class DeckAnalysisTests
{
    public DeckAnalysisTests() => TestData.LoadRealTables();

    [Fact]
    public void CountsAStartingIroncladDeck()
    {
        var deck = Enumerable.Repeat(CardFacts.Named("StrikeIronclad", CardKind.Attack), 5)
            .Concat(Enumerable.Repeat(CardFacts.Named("DefendIronclad", CardKind.Skill), 4))
            .Append(CardFacts.Named("Bash", CardKind.Attack))
            .ToList();

        var analysis = DeckAnalysis.Analyze(deck);

        Assert.Equal(10, analysis.TotalCards);
        Assert.Equal(6, analysis.Counts[Job.FrontloadedDamage]);
        Assert.Equal(4, analysis.Counts[Job.FrontloadedBlock]);
        Assert.Empty(analysis.UnclassifiedCardNames);
        Assert.Empty(analysis.GuessedCardNames);
    }

    [Fact]
    public void CountsOneCardTowardEveryJobItPerforms()
    {
        // Shrug It Off is both block and draw, so it must appear in both buckets.
        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("ShrugItOff", CardKind.Skill) });

        Assert.Equal(1, analysis.TotalCards);
        Assert.Equal(1, analysis.Counts[Job.FrontloadedBlock]);
        Assert.Equal(1, analysis.Counts[Job.CardDraw]);
    }

    [Fact]
    public void CountsCursesSeparatelyAndNotTowardAnyJob()
    {
        var deck = new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            TestData.Curse("Regret"),
            TestData.Status("Burn"),
        };

        var analysis = DeckAnalysis.Analyze(deck);

        Assert.Equal(3, analysis.TotalCards);
        Assert.Equal(2, analysis.IgnoredCards);
        Assert.Equal(1, analysis.Counts[Job.FrontloadedDamage]);
        Assert.Empty(analysis.UnclassifiedCardNames);
    }

    [Fact]
    public void ReportsGuessedCardsSeparatelyFromCuratedOnes()
    {
        var guessable = TestData.Attack(TestData.UnknownCardName, 7) with { DisplayName = "Mystery Blade" };

        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack), guessable });

        Assert.Equal(2, analysis.Counts[Job.FrontloadedDamage]);
        Assert.Equal(1, analysis.GuessedCounts[Job.FrontloadedDamage]);
        Assert.Equal(new[] { "Mystery Blade" }, analysis.GuessedCardNames);
    }

    [Fact]
    public void ListsCardsThatEvenTheHeuristicCannotPlace()
    {
        var unplaceable = CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { DisplayName = "Odd Trinket" };

        var analysis = DeckAnalysis.Analyze(new[] { unplaceable });

        Assert.Equal(new[] { "Odd Trinket" }, analysis.UnclassifiedCardNames);
        Assert.Empty(analysis.GuessedCardNames);
    }

    [Fact]
    public void AnEmptyDeckProducesZeroes()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        Assert.Equal(0, analysis.TotalCards);
        Assert.All(JobInfo.All, job => Assert.Equal(0, analysis.Counts[job]));
    }

    [Fact]
    public void PercentagesAreOfTheWholeDeck()
    {
        var deck = Enumerable.Repeat(CardFacts.Named("StrikeIronclad", CardKind.Attack), 9)
            .Concat(Enumerable.Repeat(CardFacts.Named("DefendIronclad", CardKind.Skill), 55))
            .ToList();

        var analysis = DeckAnalysis.Analyze(deck);

        // 9 of 64 is 14.06%, which must read as 14 rather than being truncated or over-rounded.
        Assert.Equal(64, analysis.TotalCards);
        Assert.Equal(14, analysis.PercentFor(Job.FrontloadedDamage));
    }

    [Fact]
    public void PercentagesCountCursesInTheDeckSize()
    {
        var deck = new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            TestData.Curse("Regret"),
        };

        // The curse cannot do a job, but it is still a card you draw, so this is 50% not 100%.
        Assert.Equal(50, DeckAnalysis.Analyze(deck).PercentFor(Job.FrontloadedDamage));
    }

    [Fact]
    public void AnEmptyDeckHasNoPercentagesRatherThanDividingByZero()
    {
        var analysis = DeckAnalysis.Analyze(Array.Empty<CardFacts>());

        Assert.All(JobInfo.All, job => Assert.Equal(0, analysis.PercentFor(job)));
    }

    [Fact]
    public void HalfPercentagesRoundUpRatherThanToEven()
    {
        // 1 of 8 is 12.5%. Banker's rounding would give 12; away-from-zero gives 13 consistently.
        var deck = Enumerable.Repeat(CardFacts.Named("StrikeIronclad", CardKind.Attack), 1)
            .Concat(Enumerable.Repeat(CardFacts.Named("DefendIronclad", CardKind.Skill), 7))
            .ToList();

        Assert.Equal(13, DeckAnalysis.Analyze(deck).PercentFor(Job.FrontloadedDamage));
    }

    [Fact]
    public void IdenticalDecksCompareEqualSoTheOverlayDoesNotRedraw()
    {
        var deck = new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) };

        Assert.Equal(DeckAnalysis.Analyze(deck), DeckAnalysis.Analyze(deck));
    }

    [Fact]
    public void AddingACardMakesTheAnalysisCompareUnequal()
    {
        // This is what triggers the overlay refresh; if it broke, counts would freeze mid-run.
        var before = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        var after = DeckAnalysis.Analyze(new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            CardFacts.Named("Bash", CardKind.Attack),
        });

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void SwappingOneCardForAnotherJobChangesTheAnalysis()
    {
        var damage = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        var block = DeckAnalysis.Analyze(new[] { CardFacts.Named("DefendIronclad", CardKind.Skill) });

        Assert.NotEqual(damage, block);
    }
}
