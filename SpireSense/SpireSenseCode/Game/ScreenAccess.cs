using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Answers whether the counts are worth showing on whatever the player is currently looking at.
///
/// The game already keeps a notion of the topmost thing the player is interacting with, ranking
/// modals, capstone screens such as the deck and discard views, the map, the overlay stack and the
/// settings menu all above the room underneath. Asking it for the current screen means this mod
/// does not have to enumerate every way of leaving combat, including ways added by a later update.
/// </summary>
public static class ScreenAccess
{
    private static bool _warned;

    /// <summary>
    /// True on the combat screen, and on the screens where you are choosing a card to add to your
    /// deck. Those are the moments the counts inform a decision: picking a reward is exactly when
    /// you want to know the deck you are picking for.
    ///
    /// Deliberately excluded are the screens for choosing a card already in your deck, to upgrade,
    /// remove or transform. Those share a base class with the in-combat draw and discard pile
    /// pickers, which must stay hidden, so they cannot be told apart by base class alone.
    /// </summary>
    public static bool ShouldShowOverlay
    {
        get
        {
            try
            {
                var screen = ActiveScreenContext.Instance?.GetCurrentScreen();

                return screen is
                    // Combat inside an event is an NCombatRoom too, so this covers both.
                    NCombatRoom
                    // The reward after a fight.
                    or NCardRewardSelectionScreen
                    // Neow, events and shops offering a card or a bundle.
                    or NChooseACardSelectionScreen
                    or NChooseABundleSelectionScreen;
            }
            catch (Exception ex)
            {
                if (!_warned)
                {
                    _warned = true;
                    ModLog.Warn($"Could not read the active screen, overlay will stay visible: {ex.Message}");
                }

                // Failing open is the kinder default: a panel that will not hide beats one that
                // will not appear.
                return true;
            }
        }
    }
}
