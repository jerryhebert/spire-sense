namespace SpireSense.SpireSenseCode.Categories;

/// <summary>An attack sitting in your hand: what it costs, and what it would actually land for.</summary>
public readonly record struct PlayableAttack(int Cost, int Damage);

/// <summary>
/// What this turn looks like: what is coming at you, and the most you could hit back for.
///
/// Both numbers come from the game's own arithmetic rather than from the cards' printed values, so
/// Strength, Vulnerable, Weak, relics, powers and enchantments are already in them.
/// </summary>
public readonly record struct TurnForecast(
    bool HasCombat,
    int IncomingDamage,
    int Block,
    int OutgoingDamage,
    int AttacksPlayed,
    int AttacksInHand,
    int Energy)
{
    public static TurnForecast None => new(false, 0, 0, 0, 0, 0, 0);

    /// <summary>What would actually reach your health, once the block you are holding is spent.</summary>
    public int AfterBlock => Math.Max(0, IncomingDamage - Block);

    /// <summary>True when energy stopped you playing every attack you are holding.</summary>
    public bool EnergyLimited => AttacksPlayed < AttacksInHand;
}

/// <summary>
/// The most damage a hand can do with the energy available.
///
/// Solved exactly rather than by taking the best damage per energy first, which gets the wrong
/// answer often enough to matter: with 3 energy, one 2-cost attack for 20 beats three 1-cost
/// attacks for 6 each, and a ratio-greedy pick takes the wrong one. The numbers here are tiny - a
/// hand of ten against a dozen energy at the very most - so the exact answer is free.
/// </summary>
public static class BestHand
{
    /// <summary>
    /// Total damage of the best affordable set of attacks, and how many of them that was.
    ///
    /// Zero-cost attacks are always in it. A negative or absurd energy total is treated as none,
    /// since the panel should go quiet rather than print something invented.
    /// </summary>
    public static int Damage(IReadOnlyList<PlayableAttack> attacks, int energy, out int played)
    {
        played = 0;
        if (attacks.Count == 0 || energy < 0)
        {
            return 0;
        }

        // dp[e] is the best damage reachable spending exactly up to e energy, and count[e] the
        // number of cards in that set, kept alongside so the panel can say when energy is the limit.
        var dp = new int[energy + 1];
        var count = new int[energy + 1];

        foreach (var attack in attacks)
        {
            if (attack.Damage <= 0)
            {
                continue;
            }

            if (attack.Cost <= 0)
            {
                // Free: it belongs in every set, so it lifts the whole table rather than competing.
                for (var e = 0; e <= energy; e++)
                {
                    dp[e] += attack.Damage;
                    count[e]++;
                }
                played = count[energy];
                continue;
            }

            if (attack.Cost > energy)
            {
                continue;
            }

            for (var e = energy; e >= attack.Cost; e--)
            {
                var withIt = dp[e - attack.Cost] + attack.Damage;
                if (withIt > dp[e])
                {
                    dp[e] = withIt;
                    count[e] = count[e - attack.Cost] + 1;
                }
            }
        }

        played = count[energy];
        return dp[energy];
    }
}
