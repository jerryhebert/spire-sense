namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The arithmetic behind the Cycle figures.
///
/// A cycle is one whole pass through the deck, and its length in turns is deck size divided by
/// cards drawn per turn. That division is deliberately fractional: 15 cards at 5 draw is 3 turns,
/// and 15 cards at 7 draw is 15/7, not 3. Rounding up would overstate every deck whose size is not
/// a multiple of its draw.
/// </summary>
public static class CycleEstimate
{
    /// <summary>Cards drawn at the start of a turn before relics and powers change it.</summary>
    public const double BaseCardsDrawnPerTurn = 5;

    /// <summary>Energy per turn, before relics or powers.</summary>
    public const int EnergyPerTurn = 3;

    /// <summary>
    /// How many turns one pass through the deck takes. Fractional on purpose; see the class note.
    /// </summary>
    public static double TurnsPerCycle(int deckSize, double cardsDrawnPerTurn = BaseCardsDrawnPerTurn)
    {
        if (deckSize <= 0)
        {
            return 0;
        }

        var draw = cardsDrawnPerTurn > 0 ? cardsDrawnPerTurn : BaseCardsDrawnPerTurn;
        return deckSize / draw;
    }

    /// <summary>
    /// The share of the deck you can afford to play in one cycle. A cheap deck plays in full and
    /// returns 1; an expensive one is throttled by energy.
    /// </summary>
    public static double PlayableFraction(int deckSize, int totalEnergyCost, double cardsDrawnPerTurn = BaseCardsDrawnPerTurn)
    {
        if (totalEnergyCost <= 0)
        {
            return 1.0;
        }

        var energyPerCycle = EnergyPerTurn * TurnsPerCycle(deckSize, cardsDrawnPerTurn);
        return energyPerCycle <= 0 ? 1.0 : Math.Min(1.0, energyPerCycle / totalEnergyCost);
    }

    /// <summary>
    /// What a whole-deck total is worth over one cycle, throttled by what the energy budget allows.
    /// Since a cycle plays the deck once, this is simply the total the energy lets you reach.
    /// </summary>
    public static double PerCycle(double deckTotal, int deckSize, int totalEnergyCost, double cardsDrawnPerTurn = BaseCardsDrawnPerTurn)
    {
        if (deckSize <= 0 || deckTotal <= 0)
        {
            return 0;
        }

        return deckTotal * PlayableFraction(deckSize, totalEnergyCost, cardsDrawnPerTurn);
    }

    /// <summary>
    /// Spreads a whole-cycle total across the turns that cycle takes. Adding dead cards lengthens
    /// the cycle without adding to the total, so this is where deck bloat shows up as a smaller
    /// number.
    /// </summary>
    public static double PerTurnOfCycle(double cycleTotal, double cycleTurns) =>
        cycleTurns <= 0 || cycleTotal <= 0 ? 0 : cycleTotal / cycleTurns;
}
