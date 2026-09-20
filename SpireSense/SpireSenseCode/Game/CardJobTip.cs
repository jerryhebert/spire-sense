using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SpireSense.SpireSenseCode.Jobs;
using SpireSense.SpireSenseCode.Overlay;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Appends a "Spire Sense" entry to a card's hover tips naming the jobs it is classified under.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class CardJobTip
{
    // Title and Description are auto-properties with private setters, so the backing fields are the
    // only way to build a HoverTip that does not come from a localization table. The mod ships no
    // .pck and therefore has no loc entries of its own.
    private static readonly FieldInfo? TitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? DescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private const string TipTitle = "Spire Sense";

    // The getter is called very often, so tips are built once per card and reused.
    private static readonly Dictionary<string, IHoverTip> Cache = new(StringComparer.Ordinal);

    /// <summary>Drops cached tips so a reclassification shows up immediately.</summary>
    public static void InvalidateCache(string? cardClassName = null)
    {
        if (cardClassName == null)
        {
            Cache.Clear();
        }
        else
        {
            Cache.Remove(cardClassName);
        }
    }

    public static bool IsAvailable => TitleField != null && DescriptionField != null;

    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            if (!IsAvailable || !OverlaySettings.Current.ShowCardTips)
            {
                return;
            }

            var tip = Build(__instance);
            if (tip != null)
            {
                __result = __result.Append(tip);
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not add the job hover tip: {ex.Message}");
        }
    }

    private static IHoverTip? Build(CardModel card)
    {
        var className = card.GetType().Name;
        if (Cache.TryGetValue(className, out var cached))
        {
            return cached;
        }

        var facts = CardFactsReader.Read(card);
        var result = CardClassifier.Classify(facts);
        if (result.Source == ClassificationSource.Ignored)
        {
            return null;
        }

        var boxed = Activator.CreateInstance(typeof(HoverTip))!;
        TitleField!.SetValue(boxed, TipTitle);
        DescriptionField!.SetValue(boxed, Describe(result));

        var tip = (HoverTip)boxed;
        tip.Id = "spiresense_jobs_" + className;
        tip.IsInstanced = true;

        Cache[className] = tip;
        return tip;
    }

    private static string Describe(Classification result)
    {
        var jobs = JobInfo.All
            .Where(j => result.Jobs.Contains(j))
            .Select(j => JobInfo.DisplayName(j).Trim())
            .ToList();

        var body = jobs.Count == 0 ? "No job" : string.Join(", ", jobs);

        var provenance = result.Source switch
        {
            ClassificationSource.Override => "your classification",
            ClassificationSource.Heuristic => "guessed, not in the tables",
            _ => null,
        };

        return provenance == null ? body : $"{body}  ({provenance})";
    }
}
