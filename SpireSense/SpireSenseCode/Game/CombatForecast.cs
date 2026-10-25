using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using SpireSense.SpireSenseCode.Categories;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Reads this turn out of the live fight: what the enemies have announced, and the most your hand
/// could hit back for.
///
/// Every number is the game's own. Enemy damage comes from the same call the game uses to pick an
/// intent icon, and card damage from the value the game already worked out to print on the card.
/// Both have been through the modifier pipeline, so Strength, Vulnerable, Weak, relics, powers and
/// enchantments are in them without this file knowing any of those exist.
/// </summary>
public static class CombatForecast
{
    public static TurnForecast Read()
    {
        try
        {
            var me = RunAccess.LocalPlayer;
            var creature = me?.Creature;
            var combat = creature?.CombatState;
            var hand = me?.PlayerCombatState?.Hand;

            if (me == null || creature == null || combat == null || hand == null || creature.IsDead)
            {
                return TurnForecast.None;
            }

            var enemies = combat.HittableEnemies;
            if (enemies.Count == 0)
            {
                return TurnForecast.None;
            }

            var incoming = Incoming(enemies, creature);
            var attacks = Attacks(hand.Cards, creature, enemies[0]);
            var energy = me.PlayerCombatState.Energy;
            var damage = BestHand.Damage(attacks, energy, out var played);

            // Damage that lands whether or not you play anything: orbs firing, and relics and
            // powers that go off on a turn boundary. On a Defect this is most of the output, and
            // leaving it out made the figure badly wrong.
            damage += Automatic(me, creature);

            return new TurnForecast(
                HasCombat: true,
                IncomingDamage: incoming,
                Block: creature.Block,
                OutgoingDamage: damage,
                AttacksPlayed: played,
                AttacksInHand: attacks.Count,
                Energy: energy);
        }
        catch (Exception ex)
        {
            // The panel is not worth a crash, and a fight the mod cannot read is better shown as
            // nothing than as a confident wrong number.
            ModLog.Warn($"Could not read this turn's figures: {ex.Message}");
            return TurnForecast.None;
        }
    }

    /// <summary>
    /// Everything the enemies have said they will do to you. Only attacks count: a buff or a debuff
    /// announces no damage, and an enemy that is about to die still swings.
    /// </summary>
    private static int Incoming(IReadOnlyList<Creature> enemies, Creature me)
    {
        var targets = new[] { me };
        var total = 0;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.Monster is not { } monster)
            {
                continue;
            }

            foreach (var intent in monster.NextMove.Intents.OfType<AttackIntent>())
            {
                try
                {
                    total += intent.GetTotalDamage(targets, enemy);
                }
                catch (Exception ex)
                {
                    // One unreadable intent must not hide the rest of the turn.
                    ModLog.Warn($"Could not read an intent on {enemy.Monster?.Id}: {ex.Message}");
                }
            }
        }

        return total;
    }

    /// <summary>
    /// The attacks in hand you could actually play, with what each would land for.
    ///
    /// Measured against the first enemy you could hit. A card that hits everything would do more in
    /// total, but the number worth reading is what you can put into one target, which is what
    /// decides whether something dies this turn.
    /// </summary>
    private static List<PlayableAttack> Attacks(IReadOnlyList<CardModel> hand, Creature me, Creature target)
    {
        var attacks = new List<PlayableAttack>();

        foreach (var card in hand)
        {
            if (card.Type != CardType.Attack)
            {
                continue;
            }

            try
            {
                // An X-cost attack eats whatever energy is left, so it cannot be costed up front
                // and would make the whole figure a guess.
                if (card.EnergyCost?.CostsX == true || !card.CanPlayTargeting(target))
                {
                    continue;
                }

                var damage = DamageOf(card);
                if (damage <= 0)
                {
                    continue;
                }

                attacks.Add(new PlayableAttack(Math.Max(0, card.EnergyCost?.Canonical ?? 0), damage));
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Could not read the attack {card.GetType().Name}: {ex.Message}");
            }
        }

        return attacks;
    }

    /// <summary>
    /// Damage that arrives this turn without you playing a card: orbs, and the relics and powers
    /// that fire on a turn boundary.
    /// </summary>
    private static int Automatic(Player me, Creature mine)
    {
        var total = 0;

        try
        {
            total += OrbDamage(me);
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not read orb damage: {ex.Message}");
        }

        try
        {
            total += PerTurnDamage(me.Relics, r => r.DynamicVars, "relic");
            total += PerTurnDamage(mine.Powers, p => p.DynamicVars, "power");
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not read per-turn damage: {ex.Message}");
        }

        return total;
    }

    /// <summary>
    /// What the channelled orbs will do on their own.
    ///
    /// PassiveVal is already run through the game's orb-value hook by each orb, so Focus is in it.
    /// Only Lightning and Glass deal damage passively: Frost makes block, Plasma makes energy, and
    /// Dark only grows until it is evoked, so none of those belong in a damage figure.
    /// </summary>
    private static int OrbDamage(Player me)
    {
        var orbs = me.PlayerCombatState?.OrbQueue?.Orbs;
        if (orbs == null)
        {
            return 0;
        }

        var total = 0m;
        foreach (var orb in orbs)
        {
            if (orb is LightningOrb or GlassOrb)
            {
                total += orb.PassiveVal;
            }
        }

        return (int)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Relics and powers that deal damage every turn by themselves.
    ///
    /// Found rather than listed. A model qualifies when it both carries a Damage value and takes
    /// over one of the turn-boundary hooks, which together is what a thing that hits every turn
    /// looks like. Checked against the decompiled game this picks out exactly the damaging ones -
    /// Mercury Hourglass, Festive Popper, Royal Poison, Inferno, Crimson Mantle, Rolling Boulder -
    /// and nothing else, and it keeps working when the game adds another one.
    /// </summary>
    private static int PerTurnDamage<T>(IEnumerable<T> models, Func<T, DynamicVarSet> varsOf, string kind)
        where T : notnull
    {
        var total = 0m;

        foreach (var model in models)
        {
            try
            {
                if (!FiresEachTurn(model.GetType()) || !varsOf(model).TryGetValue("Damage", out var damage))
                {
                    continue;
                }

                var value = damage.PreviewValue > 0 ? damage.PreviewValue : damage.BaseValue;
                if (value > 0)
                {
                    total += value;
                    ModLog.Info($"This turn: {model.GetType().Name} ({kind}) adds {value}.");
                }
            }
            catch (Exception ex)
            {
                // One odd model must not hide the rest of the turn.
                ModLog.Warn($"Could not read {model.GetType().Name}: {ex.Message}");
            }
        }

        return (int)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    /// <summary>The turn-boundary hooks, and whether a type takes any of them over itself.</summary>
    private static readonly string[] TurnHooks =
    {
        "AfterPlayerTurnStart", "BeforePlayerTurnEnd", "AfterPlayerTurnEnd",
    };

    private static bool FiresEachTurn(Type type)
    {
        foreach (var hook in TurnHooks)
        {
            var method = type.GetMethod(hook, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.DeclaringType == type)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// What a card would land for, preferring the figure the game has already computed to print on
    /// the card over the printed base. The preview is what accounts for Strength and the rest; the
    /// base is only a fallback for a card the game has not previewed yet.
    /// </summary>
    private static int DamageOf(CardModel card)
    {
        if (!card.DynamicVars.TryGetValue("Damage", out var damage))
        {
            return 0;
        }

        var preview = (int)Math.Round(damage.PreviewValue, MidpointRounding.AwayFromZero);
        if (preview > 0)
        {
            return preview;
        }

        return (int)Math.Round(damage.BaseValue, MidpointRounding.AwayFromZero);
    }
}
