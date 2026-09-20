using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Answers whether the player is looking at the combat screen itself.
///
/// The game already keeps a notion of the topmost thing the player is interacting with, ranking
/// modals, capstone screens such as the deck and discard views, the map, the overlay stack and the
/// settings menu all above the room underneath. So asking it for the current screen and checking
/// for the combat room covers every way of leaving combat behind, including ones added later,
/// without this mod having to enumerate them.
/// </summary>
public static class ScreenAccess
{
    private static bool _warned;

    public static bool IsOnCombatScreen
    {
        get
        {
            try
            {
                // Combat inside an event is an NCombatRoom too, so one check covers both.
                return ActiveScreenContext.Instance?.GetCurrentScreen() is NCombatRoom;
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
