using SpireSense.SpireSenseCode.Jobs;
using SpireSense.SpireSenseCode.Overlay;
using Xunit;

namespace SpireSense.Tests;

public class OverlayTextTests
{
    public OverlayTextTests() => TestData.LoadRealTables();

    private static string Render(IEnumerable<CardFacts> deck, bool showCardNames = true) =>
        OverlayText.Build(DeckAnalysis.Analyze(deck), "F8", showCardNames);

    [Fact]
    public void ShowsEveryJobRowAndTheDeckSize()
    {
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Contains("Spire Sense", text);
        Assert.Contains("1 cards", text);
        foreach (var job in JobInfo.All)
        {
            Assert.Contains(JobInfo.DisplayName(job).Trim(), text);
        }
    }

    [Fact]
    public void NamesTheConfiguredToggleKey()
    {
        var text = OverlayText.Build(DeckAnalysis.Empty, "Backslash", showCardNames: true);

        Assert.Contains("Backslash hides", text);
    }

    [Fact]
    public void MentionsCursesOnlyWhenTheDeckHasSome()
    {
        var clean = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        var cursed = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack), TestData.Curse("Regret") });

        Assert.DoesNotContain("Curses / Status", clean);
        Assert.Contains("Curses / Status: 1", cursed);
    }

    [Fact]
    public void HidesCardNamesWhenTheSettingIsOff()
    {
        var deck = new[] { CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { DisplayName = "Odd Trinket" } };

        Assert.Contains("Odd Trinket", Render(deck));
        Assert.DoesNotContain("Odd Trinket", Render(deck, showCardNames: false));
    }

    [Fact]
    public void ElidesLongCardNameLists()
    {
        var deck = Enumerable.Range(0, OverlayText.MaxListedNames + 3)
            .Select(i => CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { DisplayName = $"Trinket{i}" })
            .ToList();

        var text = Render(deck);

        Assert.Contains("Trinket0", text);
        Assert.DoesNotContain("Trinket8", text);
        Assert.Contains("…", text);
    }

    [Fact]
    public void EscapesBracketsSoACardNameCannotBreakTheMarkup()
    {
        var deck = new[] { CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { DisplayName = "[color=red]evil" } };

        var text = Render(deck);

        Assert.DoesNotContain("[color=red]evil", text);
        Assert.Contains("[lb]color=red]evil", text);
    }

    [Fact]
    public void MarksGuessedCountsInline()
    {
        var deck = new[] { TestData.Attack(TestData.UnknownCardName, 7) with { DisplayName = "Mystery Blade" } };

        Assert.Contains("(1 guessed)", Render(deck));
    }
}
