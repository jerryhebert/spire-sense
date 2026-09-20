namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The two cycle numbers the overlay shows, and whether they were measured this run or predicted
/// from the deck. Measured always wins once a turn has been played: it accounts for Strength,
/// relics, powers, orbs and multi-hit attacks, none of which the static model can see.
/// </summary>
public readonly record struct CycleFigures(double Damage, double Mitigation, bool Measured)
{
    public static CycleFigures From(DeckAnalysis analysis, RunStats stats) =>
        stats.HasData
            ? new CycleFigures(stats.DamagePerTurn, stats.MitigationPerTurn, Measured: true)
            : new CycleFigures(analysis.AvgCycleDamage, analysis.AvgCycleMitigation, Measured: false);

    /// <summary>Whole numbers: these are rough figures and a decimal implies precision they lack.</summary>
    public static string Format(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");
}
