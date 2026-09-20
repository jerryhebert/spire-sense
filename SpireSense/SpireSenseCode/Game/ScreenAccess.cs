using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Answers whether the counts are worth showing on whatever the player is currently looking at.
///
/// The game already tracks the topmost thing being interacted with, ranking modals, capstone
/// screens, the map, the overlay stack and menus above the room underneath. Asking it means this
/// mod does not have to enumerate every screen in the game.
/// </summary>
public static class ScreenAccess
{
    private static bool _warned;

    /// <summary>
    /// Shown nearly everywhere during a run, and hidden only on the menus that are not about the
    /// run in front of you: settings, and the submenus that cover the compendium and the pause
    /// menu. A short list of exclusions rather than a list of permitted screens, so a screen added
    /// by a future game update shows the panel instead of silently losing it.
    /// </summary>
    public static bool ShouldShowOverlay
    {
        get
        {
            try
            {
                var screen = ActiveScreenContext.Instance?.GetCurrentScreen();

                return screen is not (
                    // The settings popup, wherever it is opened from.
                    NSettingsScreenPopup
                    // Base class of both the compendium and the pause menu.
                    or NSubmenu
                    // No run is in progress here anyway, but be explicit.
                    or NMainMenu);
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
