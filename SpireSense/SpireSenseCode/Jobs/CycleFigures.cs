namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The Cycle figures: what the deck does, divided by how long a cycle through it takes.
///
/// Cycle length is the denominator on purpose. A whole-cycle total is mathematically unmoved by
/// adding curses, because over a full pass you still play every real card: per-turn output falls
/// and the cycle lengthens by the same factor, and the two cancel. Dividing by cycle length is what
/// makes deck bloat show up, which is the whole reason for watching these numbers.
/// </summary>
public readonly record struct CycleFigures(double Damage, double Mitigation, double CycleTurns, double CardsDrawn, bool Measured)
{
    /// <summary>
    /// Measured figures win once a turn has been played: they account for Strength, relics, powers,
    /// orbs and multi-hit attacks, none of which the deck data reveals.
    /// </summary>
    public static CycleFigures From(DeckAnalysis analysis, RunStats stats)
    {
        var draw = stats.HasData ? stats.HandDrawSize : CycleEstimate.BaseCardsDrawnPerTurn;
        var turns = CycleEstimate.TurnsPerCycle(analysis.TotalCards, draw);

        if (stats.HasData)
        {
            // Already a per-turn rate, which is the deck's output divided by the turns it took.
            return new CycleFigures(
                stats.DamagePerTurn, stats.MitigationPerTurn, turns, stats.CardsDrawnPerTurn, Measured: true);
        }

        return new CycleFigures(
            CycleEstimate.PerTurnOfCycle(analysis.AvgCycleDamage, turns),
            CycleEstimate.PerTurnOfCycle(analysis.AvgCycleMitigation, turns),
            turns,
            draw,
            Measured: false);
    }

    /// <summary>Whole numbers: these are rough figures and a decimal implies precision they lack.</summary>
    public static string Format(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");

    /// <summary>Cycle length reads better with one decimal, since it is usually fractional.</summary>
    public static string FormatTurns(double turns) => turns <= 0 ? "-" : turns.ToString("0.0");

    /// <summary>Draw gets a decimal: the difference between 5.0 and 5.4 is a whole extra card
    /// every few turns, and rounding would hide it.</summary>
    public static string FormatDraw(double cards) => cards.ToString("0.0");
}
