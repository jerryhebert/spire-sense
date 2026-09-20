using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Measures what the deck actually does, by watching the two commands every damage and block in the
/// game passes through.
///
/// Both have several public overloads, but each set converges on one method that all the others
/// delegate to, so two patches see everything: damage from cards, powers, orbs, relics and pets
/// alike, with Strength and every other modifier already applied.
/// </summary>
[HarmonyPatch]
public static class CombatTracking
{
    /// <summary>
    /// The terminus every CreatureCmd.Damage overload delegates to.
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[]
    {
        typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
        typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay),
    })]
    [HarmonyPostfix]
    public static void RecordDamage(Creature? dealer, Task<IEnumerable<DamageResult>> __result)
    {
        try
        {
            if (__result == null)
            {
                return;
            }

            // The command is async, so the postfix receives the Task rather than the results.
            // Reading it has to wait for completion; a continuation avoids blocking the game, and
            // runs inline when the task finished synchronously, which is the common case.
            __result.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Accumulate(dealer, task.Result);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not record damage: {ex.Message}");
        }
    }

    /// <summary>
    /// Whether a creature's output belongs to you. In multiplayer a teammate's damage and block are
    /// theirs, not yours, and counting them made the figures read high.
    ///
    /// A pet counts as yours: it has no Player of its own but names its owner, and what Osty deals
    /// and soaks is your deck working.
    ///
    /// Poison is the awkward case. It passes no dealer at all, so in multiplayer there is no way to
    /// know whose it was, and it is counted only when you are the only player rather than credited
    /// to everyone. That under-reports a poison deck in co-op, which is the safer error.
    /// </summary>
    private static bool IsMine(Creature? creature)
    {
        var me = RunAccess.LocalPlayer;
        if (me == null)
        {
            return false;
        }

        if (creature == null)
        {
            return RunAccess.CurrentRun?.Players.Count == 1;
        }

        return ReferenceEquals(creature.Player, me) || ReferenceEquals(creature.PetOwner, me);
    }

    /// <summary>
    /// Sorts one damage call into what you dealt and what was dealt to you. Both directions matter:
    /// block is only adequate relative to the damage actually coming at you, and that cannot be
    /// read from enemy data, only watched.
    /// </summary>
    private static void Accumulate(Creature? dealer, IEnumerable<DamageResult>? results)
    {
        if (results == null)
        {
            return;
        }

        var fromMe = IsMine(dealer);
        var fromEnemy = dealer is { IsEnemy: true };

        double dealt = 0;
        double taken = 0;

        foreach (var result in results)
        {
            // UnblockedDamage includes overkill, so subtracting it leaves the HP actually
            // removed. Hitting a 5 HP enemy for 30 should count as 5, not 30.
            var landed = Math.Max(0, result.UnblockedDamage - result.OverkillDamage);

            if (fromMe && result.Receiver is { IsEnemy: true })
            {
                dealt += landed;
            }
            else if (fromEnemy && IsMine(result.Receiver))
            {
                // Only enemy damage counts as incoming. Self-damage from your own cards is a cost
                // you chose, not pressure block has to answer.
                taken += landed;
            }
        }

        RunStats.Current.AddDamage(dealt);
        RunStats.Current.AddDamageTaken(taken);
    }

    /// <summary>
    /// The terminus every CreatureCmd.GainBlock overload delegates to. The return value is the
    /// block actually gained, after Dexterity and any other modifier.
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), new[]
    {
        typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool),
    })]
    [HarmonyPostfix]
    public static void RecordBlock(Creature creature, Task<decimal> __result)
    {
        try
        {
            if (__result == null || !IsMine(creature))
            {
                return;
            }

            __result.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        RunStats.Current.AddMitigation((double)task.Result);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not record block: {ex.Message}");
        }
    }

    /// <summary>
    /// Watches the number of cards actually drawn at turn start, which is what decides how long a
    /// cycle is. Reading the hook's result picks up every relic and power that changes your draw,
    /// rather than assuming the base five.
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void RecordHandDraw(Player player, decimal __result)
    {
        try
        {
            // The hook runs for every player in the combat, so without this check a teammate's
            // draw would set your cycle length, and the last one to draw would win.
            if (!ReferenceEquals(player, RunAccess.LocalPlayer))
            {
                return;
            }

            RunStats.Current.NoteHandDraw((double)__result);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not record the hand draw: {ex.Message}");
        }
    }
}
