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

    public static string Build(DeckAnalysis a, bool showCardNames)
    {
        var sb = new StringBuilder();
        sb.Append($"[b][color={Gold}]Spire Sense[/color][/b]  [color={Dim}]{a.TotalCards} cards[/color]\n");

        AppendJobCounts(sb, a);
        AppendDivider(sb);
        AppendCycleEstimates(sb, a);
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

        sb.Append("[/table]");
    }

    /// <summary>What the deck is estimated to do with what it contains.</summary>
    private static void AppendCycleEstimates(StringBuilder sb, DeckAnalysis a)
    {
        sb.Append("[table=3]");
        AppendRow(sb, "Avg cycle damage", CycleEstimate.Format(a.AvgCycleDamage));
        AppendRow(sb, "Avg cycle mitigation", CycleEstimate.Format(a.AvgCycleMitigation));
        sb.Append("[/table]");
    }

    private static void AppendRow(StringBuilder sb, string label, string figure, string? suffix = null)
    {
        sb.Append("[cell]");
        sb.Append(Escape(label));
        sb.Append("[/cell][cell]");
        sb.Append(ColumnGap);
        sb.Append("[/cell][cell][b]");
        sb.Append(Escape(figure));
        sb.Append("[/b]");
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
        if (a.IgnoredCards > 0)
        {
            sb.Append($"\n[color={Dim}]Curses / Status: {a.IgnoredCards}[/color]");
        }

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
