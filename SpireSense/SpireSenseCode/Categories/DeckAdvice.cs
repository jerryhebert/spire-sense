namespace SpireSense.SpireSenseCode.Categories;

/// <summary>What each part of the deck is asked to do, and what it is actually delivering.</summary>
public readonly record struct DeckAdviceInputs(
    double DamagePerTurn,
    double DamageNeededPerTurn,
    double MitigationPerTurn,
    double IncomingDamagePerTurn,
    int ScalingCards,
    int ScalingNeeded,
    int AccelerationCards,
    int AccelerationNeeded);

/// <summary>The weakest area of the deck, and whether there is enough evidence to name one.</summary>
public readonly record struct DeckAdviceResult(string Weakest, double Ratio, bool HasData);

/// <summary>
/// Names the part of the deck furthest behind what this point in the run demands.
///
/// This is what survived the deck power score. The score aggregated four adequacy ratios into one
/// 0–10 number using weights, a cap and a floor that were all invented and never validated against
/// whether runs were won. The ratios themselves are defensible — each is a delivered-over-demanded
/// comparison in its own units — and the actionable content was always in which one came last, not
/// in the average. So the comparison stays and the aggregation is gone.
///
/// Two of the four are measured from the run. The other two count cards against per-act thresholds
/// that are judgement, not measurement; they are the weaker evidence and are deliberately
/// forgiving. A category is named only when something has actually been observed.
/// </summary>
public static class DeckAdvice
{
    /// <summary>How far above the requirement a category can count. Beyond this, more stops helping.</summary>
    public const double MaxAdequacy = 1.5;

    /// <summary>A category at or above this is not worth naming as a weakness.</summary>
    public const double Adequate = 1.0;

    public static DeckAdviceResult Evaluate(DeckAdviceInputs inputs)
    {
        // Measured says whether a category rests on what the run has actually done, or on counting
        // cards against a per-act judgement call.
        var parts = new (string Name, double Ratio, bool Measured)[]
        {
            ("Damage", Adequacy(inputs.DamagePerTurn, inputs.DamageNeededPerTurn), true),
            ("Block", Adequacy(inputs.MitigationPerTurn, inputs.IncomingDamagePerTurn), true),
            ("Scaling", Adequacy(inputs.ScalingCards, inputs.ScalingNeeded), false),
            ("Acceleration", Adequacy(inputs.AccelerationCards, inputs.AccelerationNeeded), false),
        };

        var known = parts.Where(p => !double.IsNaN(p.Ratio)).ToList();

        // The card-count categories are computable the instant a run exists, so on their own they
        // would have the panel naming a weakness before a card had been played.
        if (!known.Any(p => p.Measured))
        {
            return new DeckAdviceResult("", double.NaN, HasData: false);
        }

        var weakest = known.OrderBy(p => p.Ratio).First();
        return new DeckAdviceResult(weakest.Name, weakest.Ratio, HasData: true);
    }

    /// <summary>
    /// Delivered over demanded, capped. NaN on either side means "not known yet", which is not the
    /// same as zero and must not be read as a failure; that category sits the comparison out.
    /// </summary>
    private static double Adequacy(double have, double need)
    {
        if (double.IsNaN(have) || need <= 0 || double.IsNaN(need))
        {
            return double.NaN;
        }

        return Math.Clamp(have / need, 0, MaxAdequacy);
    }

    /// <summary>
    /// How many scaling cards a deck wants by a given act. Judgement rather than measurement: unlike
    /// damage and block there is nothing in the run to compare against, so these are the weakest
    /// inputs and are deliberately forgiving.
    /// </summary>
    public static int ScalingNeededForAct(int actNumber) => actNumber switch
    {
        <= 1 => 2,
        2 => 3,
        _ => 4,
    };

    public static int AccelerationNeededForAct(int actNumber) => actNumber switch
    {
        <= 1 => 1,
        2 => 2,
        _ => 3,
    };

    /// <summary>
    /// Damage a deck needs per turn to finish an elite of the given health inside a reasonable
    /// fight. Grounded in the act's real monsters rather than a guess.
    /// </summary>
    public const double TargetTurnsToKillElite = 5;

    public static double DamageNeededForElite(double averageEliteHp) =>
        averageEliteHp <= 0 ? double.NaN : averageEliteHp / TargetTurnsToKillElite;
}
