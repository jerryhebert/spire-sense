namespace SpireSense.SpireSenseCode.Categories;

/// <summary>
/// What the next hard fight is likely to cost, against what you have left.
///
/// This replaced a 0–10 "deck power" score. That number had no units and no validation: its
/// weights, its cap and its floor were all invented, and four incommensurable things were averaged
/// into one scalar, which is exactly where the information went. This says something you can check
/// against what actually happens, and act on without knowing how it was computed.
/// </summary>
public readonly record struct AttritionForecast(FightKind Kind, double HpPerFight, int CurrentHp, bool HasData)
{
    /// <summary>How many more fights of this kind you could take at the current rate.</summary>
    public double FightsRemaining => HpPerFight <= 0 ? double.PositiveInfinity : CurrentHp / HpPerFight;

    /// <summary>
    /// True when the next fight of this kind would, on current form, take more health than you
    /// have. The one case worth colouring, because it is the one that ends runs.
    /// </summary>
    public bool WouldNotSurvive => HasData && HpPerFight > CurrentHp;

    /// <summary>True when you could take it but not the one after.</summary>
    public bool Marginal => HasData && !WouldNotSurvive && FightsRemaining < 2;
}

public static class Attrition
{
    /// <summary>
    /// Picks the fight worth warning about and forecasts it.
    ///
    /// Bosses first, then elites, then ordinary fights: the forecast should be about the hardest
    /// thing you have evidence for, since that is the one that kills you. Falls back down the list
    /// when a kind has not been fought yet, and reports no data at all until something has.
    /// </summary>
    public static AttritionForecast Forecast(RunStats stats, int currentHp)
    {
        foreach (var kind in new[] { FightKind.Boss, FightKind.Elite, FightKind.Normal })
        {
            var perFight = stats.HpLostPerFight(kind);
            if (!double.IsNaN(perFight))
            {
                return new AttritionForecast(kind, perFight, currentHp, HasData: true);
            }
        }

        return new AttritionForecast(FightKind.Elite, double.NaN, currentHp, HasData: false);
    }

    /// <summary>Rounds an HP figure for display. Whole numbers: fractions of a hit point are noise.</summary>
    public static string Format(double hp) =>
        double.IsNaN(hp) ? "—" : Math.Round(hp, MidpointRounding.AwayFromZero).ToString("0");
}
