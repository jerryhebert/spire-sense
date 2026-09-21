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
    public void ShowsEveryCategoryRowAndTheDeckSize()
    {
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Contains("Spire Sense", text);
        Assert.Contains("1 cards", text);
        foreach (var category in CategoryInfo.All)
        {
            // Grouped categories appear under their heading by the short half of their name, so
            // between the heading and the row the full name is on screen either way.
            Assert.Contains(CategoryInfo.ShortName(category), text);
            Assert.Contains(CategoryInfo.Group(category) ?? CategoryInfo.ShortName(category), text);
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
    public void CursesAreNotGivenARowOfTheirOwn()
    {
        // A curse count is not something you act on: you already know you took the curse, and it
        // is a row of panel height spent saying so. Curses still count toward the deck total, so
        // they still drag every percentage down, which is the part that matters.
        var cursed = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack), TestData.Curse("Regret") });

        Assert.DoesNotContain("Curses", cursed);
        Assert.Contains("50% (1)", cursed);
    }

    [Fact]
    public void CategoriesAreGroupedUnderDamageAndBlock()
    {
        // Seven identical rows read as one flat list of unrelated facts. The two headings say what
        // the rows underneath have in common, and let those rows drop the repeated half of their
        // name: "Frontloaded" under "Damage" rather than "FL. Damage" seven rows running.
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        var damageAt = text.IndexOf("Damage", StringComparison.Ordinal);
        var blockAt = text.IndexOf("Block", StringComparison.Ordinal);
        var accelerationAt = text.IndexOf("Acceleration", StringComparison.Ordinal);

        Assert.InRange(damageAt, 0, blockAt);
        Assert.InRange(blockAt, 0, accelerationAt);

        // Two "Frontloaded" and two "Scaling" rows, one under each heading.
        Assert.Equal(2, CountOf(text, "Frontloaded"));
        Assert.Equal(2, CountOf(text, "Scaling"));
        Assert.Equal(1, CountOf(text, "AOE"));

        // The grouped rows are indented under their heading, and Acceleration is not, because it
        // has no heading to sit under.
        Assert.Contains("\u00A0Frontloaded", text);
        Assert.DoesNotContain("\u00A0Acceleration", text);
    }

    private static int CountOf(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
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

        // The padding is no-break spaces. RichTextLabel does not treat leading ordinary spaces in a
        // cell as significant, so padding with them put single-digit rows a character left of the
        // rest — which is exactly the misalignment this is meant to prevent.
        Assert.All(figures, f => Assert.DoesNotContain(' ', f.TakeWhile(char.IsWhiteSpace)));
    }

    [Fact]
    public void ThePowerScoreIsSetApartFromTheCountsBelowIt()
    {
        // It is a verdict on the whole deck rather than one more fact about it, and run straight
        // into the counts it read as just another row.
        var text = Render(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        var powerAt = text.IndexOf("Power", StringComparison.Ordinal);
        var firstDividerAt = text.IndexOf('─');
        var firstCountAt = text.IndexOf("Frontloaded", StringComparison.Ordinal);

        Assert.InRange(powerAt, 0, firstDividerAt);
        Assert.InRange(firstDividerAt, 0, firstCountAt);
    }

    [Fact]
    public void WhatIsHoldingTheScoreBackSitsOnItsOwnLine()
    {
        // The panel is only as wide as its widest line, so this one sharing a line with the score
        // set a floor on the whole panel that no amount of dragging the resize grip could get past.
        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        var power = new DeckPowerResult(6.2, "Acceleration", HasData: true);

        var text = OverlayText.Build(analysis, true, CycleFigures.From(analysis, new RunStats()), power);
        var lines = text.Split('\n');

        var scoreLine = Assert.Single(lines, l => l.Contains("Power "));
        Assert.DoesNotContain("held back by", scoreLine);

        var reasonLine = Assert.Single(lines, l => l.Contains("held back by"));
        Assert.StartsWith(" ", reasonLine);
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
