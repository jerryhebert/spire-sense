using System.Text;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>Formats a <see cref="DeckAnalysis"/> as BBCode for the overlay's RichTextLabel.</summary>
public static class OverlayText
{
    private const string Gold = "#e0c070";
    private const string Dim = "#9a9a9a";
    private const string Warn = "#e08a5a";

    /// <summary>How many card names are listed before the rest are elided.</summary>
    public const int MaxListedNames = 6;

    /// <summary>Contents of the spacer column. A table column with nothing in it collapses to nothing.</summary>
    private const string ColumnGap = "    ";

    public static string Build(DeckAnalysis a, bool showCardNames)
    {
        var sb = new StringBuilder();
        sb.Append($"[b][color={Gold}]Spire Sense[/color][/b]  [color={Dim}]{a.TotalCards} cards[/color]\n");
        // Three columns, the middle one empty, which pushes the figures clear of the longest job
        // name instead of crowding it.
        sb.Append("[table=3]");

        foreach (var job in JobInfo.All)
        {
            var count = a.Counts[job];
            var guessed = a.GuessedCounts[job];
            var name = JobInfo.DisplayName(job);
            var figure = $"{a.PercentFor(job)}% ({count})";

            sb.Append("[cell]");
            sb.Append(Escape(name));
            sb.Append("[/cell][cell]");
            sb.Append(ColumnGap);
            sb.Append("[/cell][cell]");
            sb.Append($"[b]{figure}[/b]");
            if (guessed > 0)
            {
                sb.Append($" [color={Dim}]({guessed} guessed)[/color]");
            }
            sb.Append("[/cell]");
        }

        sb.Append("[/table]");

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

        return sb.ToString();
    }

    private static string NameList(IReadOnlyList<string> names)
    {
        var shown = Escape(string.Join(", ", names.Take(MaxListedNames)));
        return names.Count > MaxListedNames ? shown + " …" : shown;
    }

    /// <summary>Neutralizes BBCode markup in card names so a stray bracket cannot break the panel.</summary>
    public static string Escape(string text) => text.Replace("[", "[lb]");
}
