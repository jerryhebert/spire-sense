using System.Text;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>Formats a <see cref="DeckAnalysis"/> as BBCode for the overlay's RichTextLabel.</summary>
public static class OverlayText
{
    private const string Gold = "#e0c070";
    private const string Dim = "#9a9a9a";
    private const string Warn = "#e08a5a";

    public static string Build(DeckAnalysis a, OverlaySettings settings)
    {
        var sb = new StringBuilder();
        sb.Append($"[b][color={Gold}]Spire Sense[/color][/b]  [color={Dim}]{a.TotalCards} cards · {settings.ToggleKey} hides[/color]\n");
        sb.Append("[table=2]");

        foreach (var job in JobInfo.All)
        {
            var count = a.Counts[job];
            var guessed = a.GuessedCounts[job];
            var name = JobInfo.DisplayName(job);
            var isSub = job == Job.FrontloadedAoe;

            sb.Append("[cell]");
            sb.Append(isSub ? $"[color={Dim}]{Escape(name)}[/color]" : Escape(name));
            sb.Append("[/cell][cell]");
            sb.Append(isSub ? $"[color={Dim}]{count}[/color]" : $"[b]{count}[/b]");
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
            if (settings.ShowCardNames)
            {
                sb.Append($" [color={Dim}]{Escape(string.Join(", ", a.UnclassifiedCardNames.Take(6)))}");
                if (a.UnclassifiedCardNames.Count > 6)
                {
                    sb.Append(" …");
                }
                sb.Append("[/color]");
            }
        }

        if (a.GuessedCardNames.Count > 0 && settings.ShowCardNames)
        {
            sb.Append($"\n[color={Dim}]Guessed: {Escape(string.Join(", ", a.GuessedCardNames.Take(6)))}");
            if (a.GuessedCardNames.Count > 6)
            {
                sb.Append(" …");
            }
            sb.Append("[/color]");
        }

        return sb.ToString();
    }

    private static string Escape(string text) => text.Replace("[", "[lb]");
}
