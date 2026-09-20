using System.Text;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>Formats a <see cref="DeckAnalysis"/> as BBCode for the overlay's RichTextLabel.</summary>
public static class OverlayText
{
    private const string Gold = "#e0c070";
    private const string Dim = "#9a9a9a";
    private const string Warn = "#e08a5a";
    private const string Divider = "#5a5a5a";

    /// <summary>How many card names are listed before the rest are elided.</summary>
    public const int MaxListedNames = 6;

    /// <summary>Contents of the spacer column. A table column with nothing in it collapses to nothing.</summary>
    private const string ColumnGap = "    ";

    private const char DividerChar = '─';
    private const int DividerWidth = 34;

    public static string Build(DeckAnalysis a, bool showCardNames, CycleFigures cycle)
    {
        var sb = new StringBuilder();
        sb.Append($"[b][color={Gold}]Spire Sense[/color][/b]  [color={Dim}]{a.TotalCards} cards[/color]\n");

        AppendJobCounts(sb, a);
        AppendDivider(sb);
        AppendCycle(sb, cycle);
        AppendFootnotes(sb, a, showCardNames);

        return sb.ToString();
    }

    /// <summary>
    /// What the deck contains. Three columns, the middle one empty, which pushes the figures clear
    /// of the longest job name instead of crowding it.
    /// </summary>
    private static void AppendJobCounts(StringBuilder sb, DeckAnalysis a)
    {
        sb.Append("[table=3]");

        foreach (var job in JobInfo.All)
        {
            var guessed = a.GuessedCounts[job];
            var figure = $"{a.PercentFor(job)}% ({a.Counts[job]})";

            AppendRow(sb, JobInfo.DisplayName(job), figure,
                guessed > 0 ? $" [color={Dim}]({guessed} guessed)[/color]" : null);
        }

        // Also a fact about what the deck contains, so it belongs with the counts rather than down
        // with the estimates. Dimmed, because unlike the rows above it is not a job.
        if (a.IgnoredCards > 0)
        {
            AppendRow(sb, "Curses / Status", $"{a.PercentOfDeck(a.IgnoredCards)}% ({a.IgnoredCards})", dim: true);
        }

        sb.Append("[/table]");
    }

    /// <summary>
    /// What the deck does per turn: measured from this run once a turn has been played, and
    /// predicted from the deck until then. The estimate is marked so the two are never confused.
    /// </summary>
    private static void AppendCycle(StringBuilder sb, CycleFigures cycle)
    {
        sb.Append($"[b][color={Gold}]Cycle[/color][/b]");

        // Cycle length is shown because it is the denominator: when it climbs, the deck has slowed,
        // and that is the part of a curse's cost the damage figure alone would not explain.
        sb.Append($"  [color={Dim}]{CycleFigures.FormatTurns(cycle.CycleTurns)} turns[/color]");

        if (!cycle.Measured)
        {
            sb.Append($"  [color={Dim}]estimated[/color]");
        }
        sb.Append('\n');

        sb.Append("[table=3]");
        AppendRow(sb, "Damage:", CycleFigures.Format(cycle.Damage));
        AppendRow(sb, "Mitigation:", CycleFigures.Format(cycle.Mitigation));
        sb.Append("[/table]");
    }

    private static void AppendRow(StringBuilder sb, string label, string figure, string? suffix = null, bool dim = false)
    {
        sb.Append("[cell]");
        sb.Append(dim ? $"[color={Dim}]{Escape(label)}[/color]" : Escape(label));
        sb.Append("[/cell][cell]");
        sb.Append(ColumnGap);
        sb.Append("[/cell][cell]");
        sb.Append(dim ? $"[color={Dim}]{Escape(figure)}[/color]" : $"[b]{Escape(figure)}[/b]");
        if (suffix != null)
        {
            sb.Append(suffix);
        }
        sb.Append("[/cell]");
    }

    /// <summary>A rule separating what the deck contains from what it is estimated to do with it.</summary>
    private static void AppendDivider(StringBuilder sb)
    {
        sb.Append('\n');
        sb.Append($"[color={Divider}]");
        sb.Append(new string(DividerChar, DividerWidth));
        sb.Append("[/color]\n");
    }

    private static void AppendFootnotes(StringBuilder sb, DeckAnalysis a, bool showCardNames)
    {
        if (a.UnclassifiedCardNames.Count > 0)
        {
            sb.Append($"\n[color={Warn}]No job: {a.UnclassifiedCardNames.Count}[/color]");
            if (showCardNames)
            {
                sb.Append($" [color={Dim}]{NameList(a.UnclassifiedCardNames)}[/color]");
            }
        }

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
