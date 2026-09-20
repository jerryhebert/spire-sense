using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SpireSense.SpireSenseCode.Jobs;
using SpireSense.SpireSenseCode.Overlay;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Attaches the reclassification panel to the card inspect screen, the one reached from the
/// Compendium and from viewing a card during a run, and keeps it pointed at the card on show.
/// </summary>
public static class InspectScreenPatch
{
    private const string PanelName = "SpireSenseJobEditor";

    // _cards and _index are private; the screen exposes no way to ask which card it is showing.
    private static readonly AccessTools.FieldRef<NInspectCardScreen, List<CardModel>?> CardsField =
        AccessTools.FieldRefAccess<NInspectCardScreen, List<CardModel>?>("_cards");
    private static readonly AccessTools.FieldRef<NInspectCardScreen, int> IndexField =
        AccessTools.FieldRefAccess<NInspectCardScreen, int>("_index");

    [HarmonyPatch(typeof(NInspectCardScreen), "_Ready")]
    [HarmonyPostfix]
    public static void AddPanel(NInspectCardScreen __instance)
    {
        try
        {
            if (__instance.GetNodeOrNull<JobEditorPanel>(PanelName) != null)
            {
                return;
            }

            var panel = new JobEditorPanel { Name = PanelName, Visible = false };
            __instance.AddChild(panel);
        }
        catch (Exception ex)
        {
            ModLog.Error($"Could not attach the job editor to the inspect screen: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
    [HarmonyPostfix]
    public static void RefreshPanel(NInspectCardScreen __instance)
    {
        try
        {
            var panel = __instance.GetNodeOrNull<JobEditorPanel>(PanelName);
            if (panel == null)
            {
                return;
            }

            var cards = CardsField(__instance);
            var index = IndexField(__instance);
            var card = cards != null && index >= 0 && index < cards.Count ? cards[index] : null;
            panel.ShowFor(card);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not update the job editor: {ex.Message}");
        }
    }
}
