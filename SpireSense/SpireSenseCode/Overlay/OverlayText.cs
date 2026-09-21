using System.Text;
using SpireSense.SpireSenseCode.Categories;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>Formats a <see cref="DeckAnalysis"/> as BBCode for the overlay's RichTextLabel.</summary>
public static class OverlayText
{
    private const string Gold = "#e0c070";
    private const string Dim = "#9a9a9a";
    private const string Warn = "#e08a5a";
    private const string Divider = "#5a5a5a";
    private const string Good = "#8fbf6f";

    /// <summary>How many card names are listed before the rest are elided.</summary>
    public const int MaxListedNames = 6;

    /// <summary>Contents of the spacer column. A table column with nothing in it collapses to nothing.</summary>
    private const string ColumnGap = "    ";

    private const char DividerChar = '─';
    private const int DividerWidth = 34;

    /// <summary>
    /// Width every figure is padded to. In the proportional UI font "9% (3)" and "18% (12)" are
    /// different widths even sharing a column, so figures are set in the mono font and padded to a
    /// common width; the digits, brackets and decimal points then line up down the whole panel.
    /// </summary>
    private const int FigureWidth = 9;

    /// <summary>
    /// What figures are padded with. A no-break space, not an ordinary one: RichTextLabel does not
    /// treat leading ordinary spaces in a cell as significant, so a row padded with three of them
    /// lost more width than a row padded with two, and single-digit rows sat a character left of
    /// the rest. U+00A0 survives that, and is the same advance width in a monospaced font.
    /// </summary>
    private const char FigurePad = ' ';

    public static string Build(DeckAnalysis a, bool showCardNames, CycleFigures cycle, DeckPowerResult power)
    {
        var sb = new StringBuilder();
        sb.Append($"[b][color={Gold}]Spire Sense[/color][/b]  [color={Dim}]{a.TotalCards} cards[/color]\n");

        AppendPower(sb, power);
        AppendDivider(sb, leadingBlankLine: false);
        AppendCategoryCounts(sb, a);
        AppendDivider(sb);
        AppendCycle(sb, cycle);
        AppendFootnotes(sb, a, showCardNames);

        return sb.ToString();
    }

    /// <summary>
    /// The headline number, and the category holding it back. The limiting category is the actionable half:
    /// it says what to draft, where the score alone says only how worried to be.
    ///
    /// Given a rule of its own below it because it is a verdict on the whole deck rather than one
    /// more fact about it; run together with the counts, it read as just another statistic.
    /// </summary>
    private static void AppendPower(StringBuilder sb, DeckPowerResult power)
    {
        if (!power.HasData)
        {
            sb.Append($"[color={Dim}]Power: measuring…[/color]");
            sb.Append('\n');
            return;
        }

        var colour = power.Score switch
        {
            >= 7.0 => Good,
            >= 4.5 => Gold,
            _ => Warn,
        };

        sb.Append($"[b][color={colour}]Power {Mono(power.Label)} / {Mono("10")}[/color][/b]");
        sb.Append($"  [color={Dim}]held back by {Escape(power.LimitedBy)}[/color]");
        sb.Append('\n');
    }

    /// <summary>
    /// What the deck contains. Three columns, the middle one empty, which pushes the figures clear
    /// of the longest category name instead of crowding it.
    /// </summary>
    private static void AppendCategoryCounts(StringBuilder sb, DeckAnalysis a)
    {
        sb.Append("[table=3]");

        foreach (var category in CategoryInfo.All)
        {
            var guessed = a.GuessedCounts[category];
            var figure = $"{a.PercentFor(category)}% ({a.Counts[category]})";

            AppendRow(sb, CategoryInfo.DisplayName(category), figure,
                guessed > 0 ? $" [color={Dim}]({guessed} guessed)[/color]" : null);
        }

        // Also a fact about what the deck contains, so it belongs with the counts rather than down
        // with the estimates. Dimmed, because unlike the rows above it is not a category.
        if (a.IgnoredCards > 0)
        {
            AppendRow(sb, "Curses / Status", $"{a.PercentOfDeck(a.IgnoredCards)}% ({a.IgnoredCards})", dim: true);
        }

        sb.Append("[/table]");
    }

    /// <summary>
    /// What the deck does per turn: measured from this run once a turn has been played, and
    /// predicted from the deck until then. The estimate is marked so the two are never confused.
    ///
    /// Per turn rather than per cycle on purpose. A cycle total is a moving target, because the
    /// cycle lengthens every time the deck grows, so the same figure means something different in
    /// Act 3 than it did in Act 1. A per-turn rate stays comparable across the whole run.
    /// </summary>
    private static void AppendCycle(StringBuilder sb, CycleFigures cycle)
    {
        sb.Append($"[b][color={Gold}]Per turn[/color][/b]");

        if (!cycle.Measured)
        {
            sb.Append($"  [color={Dim}]estimated[/color]");
        }
        sb.Append('\n');

        sb.Append("[table=3]");
        AppendRow(sb, "Damage:", CycleFigures.Format(cycle.Damage));
        AppendRow(sb, "Mitigation:", CycleFigures.Format(cycle.Mitigation));
        AppendRow(sb, "Draw:", CycleFigures.FormatDraw(cycle.CardsDrawn));
        sb.Append("[/table]");
    }

    private static void AppendRow(StringBuilder sb, string label, string figure, string? suffix = null, bool dim = false)
    {
        var value = Mono(figure.PadLeft(FigureWidth, FigurePad));

        sb.Append("[cell]");
        sb.Append(dim ? $"[color={Dim}]{Escape(label)}[/color]" : Escape(label));
        sb.Append("[/cell][cell]");
        sb.Append(ColumnGap);
        sb.Append("[/cell][cell]");
        sb.Append(dim ? $"[color={Dim}]{value}[/color]" : $"[b]{value}[/b]");
        if (suffix != null)
        {
            sb.Append(suffix);
        }
        sb.Append("[/cell]");
    }

    /// <summary>
    /// Sets text in the label's mono font. A RichTextLabel reaches that font only through [lb]code],
    /// so the tag is doing the work of a font switch here and carries none of its usual meaning.
    /// </summary>
    private static string Mono(string text) => $"[code]{Escape(text)}[/code]";

    /// <summary>A rule separating one part of the panel from the next.</summary>
    private static void AppendDivider(StringBuilder sb, bool leadingBlankLine = true)
    {
        if (leadingBlankLine)
        {
            sb.Append('\n');
        }

        sb.Append($"[color={Divider}]");
        sb.Append(new string(DividerChar, DividerWidth));
        sb.Append("[/color]\n");
    }

    /// <summary>
    /// Cards with no category are deliberately not reported. Every card in the game is curated, so
    /// the only ones that land there are the handful that genuinely touch no axis — a heal, a gold
    /// payout — and a warning about those is noise on a panel read mid-fight. The hover tip still
    /// says so on the card itself, where you asked about that card.
    /// </summary>
    private static void AppendFootnotes(StringBuilder sb, DeckAnalysis a, bool showCardNames)
    {
        if (a.GuessedCardNames.Count > 0 && showCardNames)
        {
            sb.Append($"\n[color={Dim}]Guessed: {NameList(a.GuessedCardNames)}[/color]");
        }
    }

    private static string NameList(IReadOnlyList<string> names)
    {
        var shown = Escape(string.Join(", ", names.Take(MaxListedNames)));
        return names.Count > MaxListedNames ? shown + " …" : shown;
    }

    /// <summary>Neutralizes BBCode markup in card names so a stray bracket cannot break the panel.</summary>
    public static string Escape(string text) => text.Replace("[", "[lb]");
}
