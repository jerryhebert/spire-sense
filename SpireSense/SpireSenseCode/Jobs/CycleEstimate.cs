namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// Rough per-turn output of a deck, averaged over one full cycle through it.
///
/// A deliberately simple model, because a precise one is not possible from card data alone:
/// a cycle lasts as many turns as it takes to draw the whole deck, you have a fixed energy budget
/// over those turns, and if the deck costs more energy than that budget you only get through part
/// of it. Dead cards still lengthen the cycle and so dilute the average, which is the behaviour you
/// want: adding a curse really does reduce your damage per turn.
///
/// Known to under-report multi-hit attacks, which expose only their per-hit damage, and to ignore
/// Strength, Dexterity and other multipliers, which are not knowable outside a fight.
/// </summary>
public static class CycleEstimate
{
    /// <summary>Cards drawn at the start of a turn, before any draw effects.</summary>
    public const int CardsDrawnPerTurn = 5;

    /// <summary>Energy per turn, before relics or powers.</summary>
    public const int EnergyPerTurn = 3;

    /// <summary>How many turns it takes to draw the whole deck once.</summary>
    public static int TurnsPerCycle(int deckSize) =>
        deckSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(deckSize / (double)CardsDrawnPerTurn));

    /// <summary>
    /// The share of the deck you can actually afford to play in one cycle. A cheap deck plays in
    /// full and returns 1; an expensive one is throttled by energy.
    /// </summary>
    public static double PlayableFraction(int deckSize, int totalEnergyCost)
    {
        if (totalEnergyCost <= 0)
        {
            return 1.0;
        }

        var energyPerCycle = (double)EnergyPerTurn * TurnsPerCycle(deckSize);
        return Math.Min(1.0, energyPerCycle / totalEnergyCost);
    }

    /// <summary>
    /// Averages a whole-deck total into a per-turn figure, throttled by what the energy budget
    /// allows. Used for both damage and mitigation, which share the model exactly.
    /// </summary>
    public static double PerTurn(double deckTotal, int deckSize, int totalEnergyCost)
    {
        if (deckSize <= 0 || deckTotal <= 0)
        {
            return 0;
        }

        return deckTotal * PlayableFraction(deckSize, totalEnergyCost) / TurnsPerCycle(deckSize);
    }

    /// <summary>Renders an estimate for display, e.g. "12.4".</summary>
    public static string Format(double value) => value.ToString("0.0");
}
