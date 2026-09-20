namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The two cycle numbers the overlay shows, and whether they were measured this run or predicted
/// from the deck. Measured always wins once a turn has been played: it accounts for Strength,
/// relics, powers, orbs and multi-hit attacks, none of which the static model can see.
/// </summary>
public readonly record struct CycleFigures(double Damage, double Mitigation, bool Measured)
{
    /// <summary>
    /// Both sources are converted to one whole pass through the deck. Measured rates are per turn,
    /// so they are multiplied by the cycle length; the deck estimate is already a whole-deck total.
    /// </summary>
    public static CycleFigures From(DeckAnalysis analysis, RunStats stats)
    {
        if (!stats.HasData)
        {
            return new CycleFigures(analysis.AvgCycleDamage, analysis.AvgCycleMitigation, Measured: false);
        }

        var draw = stats.CardsDrawnPerTurn;
        return new CycleFigures(
            CycleEstimate.FromPerTurn(stats.DamagePerTurn, analysis.TotalCards, draw),
            CycleEstimate.FromPerTurn(stats.MitigationPerTurn, analysis.TotalCards, draw),
            Measured: true);
    }

    /// <summary>Whole numbers: these are rough figures and a decimal implies precision they lack.</summary>
    public static string Format(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");
}
