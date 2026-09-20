using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
            if (dealer is not { IsPlayer: true } || __result == null)
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
                        Accumulate(task.Result);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not record damage: {ex.Message}");
        }
    }

    private static void Accumulate(IEnumerable<DamageResult>? results)
    {
        if (results == null)
        {
            return;
        }

        double dealt = 0;
        foreach (var result in results)
        {
            if (result.Receiver is not { IsEnemy: true })
            {
                continue;
            }

            // UnblockedDamage includes overkill, so subtracting it leaves the HP actually
            // removed. Hitting a 5 HP enemy for 30 should count as 5, not 30.
            dealt += Math.Max(0, result.UnblockedDamage - result.OverkillDamage);
        }

        RunStats.Current.AddDamage(dealt);
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
            if (creature is not { IsPlayer: true } || __result == null)
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
}
