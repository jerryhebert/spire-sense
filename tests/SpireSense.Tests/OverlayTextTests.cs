using SpireSense.SpireSenseCode.Categories;
using SpireSense.SpireSenseCode.Overlay;
using Xunit;

namespace SpireSense.Tests;

public class OverlayTextTests
{
    public OverlayTextTests() => TestData.LoadRealTables();

    private static string Render(IEnumerable<CardFacts> deck, bool showCardNames = true)
    {
        var analysis = DeckAnalysis.Analyze(deck);
        return OverlayText.Build(analysis, showCardNames, CycleFigures.From(analysis, new RunStats()), default);
    }

    [Fact]
    public void ShowsEveryJobRowAndTheDeckSize()
    {
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Contains("Spire Sense", text);
        Assert.Contains("1 cards", text);
        foreach (var category in CategoryInfo.All)
        {
            Assert.Contains(CategoryInfo.DisplayName(category).Trim(), text);
        }
    }

    [Fact]
    public void DoesNotRepeatTheHotkeyInTheHeader()
    {
        // The rebind button under the counts shows the current key, so the header must not
        // duplicate it and go stale after a rebind.
        var text = OverlayText.Build(DeckAnalysis.Empty, showCardNames: true, CycleFigures.From(DeckAnalysis.Empty, new RunStats()), default);

        Assert.Contains("0 cards", text);
        Assert.DoesNotContain("hides", text);
    }

    [Fact]
    public void MentionsCursesOnlyWhenTheDeckHasSome()
    {
        var clean = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        var cursed = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack), TestData.Curse("Regret") });

        Assert.DoesNotContain("Curses / Status", clean);
        Assert.Contains("Curses / Status", cursed);
        Assert.Contains("50% (1)", cursed);
    }

    [Fact]
    public void CursesAreCountedWithTheDeckNotWithTheEstimates()
    {
        // They describe what the deck contains, so they belong above the divider with the category
        // counts rather than below it with what the deck does.
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack), TestData.Curse("Regret") });

        var cursesAt = text.IndexOf("Curses / Status", StringComparison.Ordinal);
        // The last rule is the one between the counts and the estimates; an earlier one sets the
        // power score apart at the top of the panel.
        var dividerAt = text.LastIndexOf('─');
        var estimatesAt = text.IndexOf("Per turn", StringComparison.Ordinal);

        Assert.InRange(cursesAt, 0, dividerAt);
        Assert.InRange(dividerAt, 0, estimatesAt);
    }

    [Fact]
    public void FiguresAreSetInTheMonoFontSoTheirColumnsLineUp()
    {
        // The UI font is proportional, so padding alone leaves "9% (3)" and "18% (12)" different
        // widths. Every figure is wrapped for the label's mono font and padded to one width; drop
        // either half and the panel's numbers stop lining up.
        var text = Render(new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            CardFacts.Named("DefendIronclad", CardKind.Skill),
        });

        var figures = System.Text.RegularExpressions.Regex
            .Matches(text, @"\[code\](.*?)\[/code\]")
            .Select(m => m.Groups[1].Value)
            .ToList();

        Assert.NotEmpty(figures);
        Assert.Contains(figures, f => f.Contains('%'));
        Assert.All(figures.Where(f => f.Contains('%')), f => Assert.Equal(figures.First(x => x.Contains('%')).Length, f.Length));
    }

    [Fact]
    public void ThePowerScoreIsSetApartFromTheCountsBelowIt()
    {
        // It is a verdict on the whole deck rather than one more fact about it, and run straight
        // into the counts it read as just another row.
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        var powerAt = text.IndexOf("Power", StringComparison.Ordinal);
        var firstDividerAt = text.IndexOf('─');
        var firstCountAt = text.IndexOf("FL. Damage", StringComparison.Ordinal);

        Assert.InRange(powerAt, 0, firstDividerAt);
        Assert.InRange(firstDividerAt, 0, firstCountAt);
    }

    [Fact]
    public void CardsWithNoCategoryAreNotReportedOnThePanel()
    {
        // Every card in the game is curated, so the only ones with no category are the handful that
        // genuinely touch no axis: a heal, a gold payout. Flagging those mid-fight is noise. The
        // hover tip still says so on the card itself, where the question was actually asked.
        var deck = new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            CardFacts.Named(TestData.UnknownCardName, CardKind.Skill) with { DisplayName = "Does Nothing" },
        };

        var text = Render(deck);

        Assert.DoesNotContain("No category", text);
        Assert.DoesNotContain("Does Nothing", text);
    }

    [Fact]
    public void HidesCardNamesWhenTheSettingIsOff()
    {
        var deck = new[] { TestData.Skill(TestData.UnknownCardName, block: 5) with { DisplayName = "Odd Trinket" } };

        Assert.Contains("Odd Trinket", Render(deck));
        Assert.DoesNotContain("Odd Trinket", Render(deck, showCardNames: false));
    }

    [Fact]
    public void ElidesLongCardNameLists()
    {
        var deck = Enumerable.Range(0, OverlayText.MaxListedNames + 3)
            .Select(i => TestData.Skill(TestData.UnknownCardName, block: 5) with { DisplayName = $"Trinket{i}" })
            .ToList();

        var text = Render(deck);

        Assert.Contains("Trinket0", text);
        Assert.DoesNotContain("Trinket8", text);
        Assert.Contains("…", text);
    }

    [Fact]
    public void EscapesBracketsSoACardNameCannotBreakTheMarkup()
    {
        var deck = new[] { TestData.Skill(TestData.UnknownCardName, block: 5) with { DisplayName = "[color=red]evil" } };

        var text = Render(deck);

        Assert.DoesNotContain("[color=red]evil", text);
        Assert.Contains("[lb]color=red]evil", text);
    }

    [Fact]
    public void ShowsAPercentageAlongsideTheCount()
    {
        // 1 of 4 cards does frontloaded damage.
        var deck = new[]
        {
            CardFacts.Named("StrikeIronclad", CardKind.Attack),
            CardFacts.Named("DefendIronclad", CardKind.Skill),
            CardFacts.Named("DefendIronclad", CardKind.Skill),
            CardFacts.Named("DefendIronclad", CardKind.Skill),
        };

        var text = Render(deck);

        Assert.Contains("25% (1)", text);
        Assert.Contains("75% (3)", text);
    }

    [Fact]
    public void PutsTheFiguresInAThirdColumn()
    {
        // A spacer column keeps the numbers clear of the longest category name.
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Contains("[table=3]", text);
    }

    [Fact]
    public void MarksGuessedCountsInline()
    {
        var deck = new[] { TestData.Attack(TestData.UnknownCardName, 7) with { DisplayName = "Mystery Blade" } };

        Assert.Contains("(1 guessed)", Render(deck));
    }
}
