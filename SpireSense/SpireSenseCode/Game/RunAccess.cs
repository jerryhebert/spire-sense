using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Read-only access to the current run and the local player's deck.
/// RunManager keeps its RunState private, so it is read through reflection.
/// </summary>
public static class RunAccess
{
    private static readonly PropertyInfo? StateProperty = AccessTools.Property(typeof(RunManager), "State");
    private static bool _warnedMissingState;

    public static RunState? CurrentRun
    {
        get
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.IsInProgress)
            {
                return null;
            }

            if (StateProperty == null)
            {
                if (!_warnedMissingState)
                {
                    _warnedMissingState = true;
                    SpireSenseMod.Logger.Error("RunManager.State property not found; the game version may be incompatible with this mod.");
                }
                return null;
            }

            try
            {
                return StateProperty.GetValue(manager) as RunState;
            }
            catch
            {
                return null;
            }
        }
    }

    public static Player? LocalPlayer
    {
        get
        {
            var run = CurrentRun;
            if (run == null)
            {
                return null;
            }

            try
            {
                var me = LocalContext.GetMe(run);
                if (me != null)
                {
                    return me;
                }
            }
            catch
            {
                // Falls through to the single-player default below.
            }

            return run.Players.Count > 0 ? run.Players[0] : null;
        }
    }

    public static IReadOnlyList<CardModel>? LocalDeck => LocalPlayer?.Deck?.Cards;
}
