using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
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
