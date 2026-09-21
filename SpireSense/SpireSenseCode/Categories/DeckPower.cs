namespace SpireSense.SpireSenseCode.Categories;

/// <summary>What each part of the deck is asked to do, and what it is actually delivering.</summary>
public readonly record struct DeckPowerInputs(
    double DamagePerTurn,
    double DamageNeededPerTurn,
    double MitigationPerTurn,
    double DamageTakenPerTurn,
    int ScalingCards,
    int ScalingNeeded,
    int AccelerationCards,
    int AccelerationNeeded);

/// <summary>A score out of ten, and the category dragging it down.</summary>
public readonly record struct DeckPowerResult(double Score, string LimitedBy, bool HasData)
{
    /// <summary>The score as it is shown, one decimal place.</summary>
    public string Label => Score.ToString("0.0");
}

/// <summary>
/// One number for how well the deck is holding up, built to match how runs are actually lost.
///
/// Deliberately not a weighted sum. A sum lets enormous damage paper over having no block, which is
/// exactly how people die; the framework's claim is that you lose to the category you are *missing*. So
/// each category gets an adequacy ratio, what it delivers over what this point in the run demands, and
/// those are combined with a harmonic mean, which is dominated by the smallest of them.
///
/// The result is put on a 1–10 scale, because an unbounded-looking number gives you nothing to
/// judge it against. Meeting every demand exactly scores 7: comfortable, with headroom left for a
/// deck that is genuinely ahead of the curve rather than merely keeping up.
///
/// Surplus is capped. Twice the damage you need cannot buy off having half the block you need,
/// because in a fight it genuinely cannot.
///
/// The limiting category matters more than the number. "6.2, held back by Block" tells you what to
/// draft; "6.2" on its own tells you nothing you can act on.
/// </summary>
public static class DeckPower
{
    /// <summary>How far above the requirement a category can count. Beyond this, more stops helping.</summary>
    public const double MaxAdequacy = 1.5;

    /// <summary>Ends of the displayed scale. One rather than zero, so the floor still reads as a score.</summary>
    public const double MinScore = 1.0;
    public const double MaxScore = 10.0;

    public static DeckPowerResult Evaluate(DeckPowerInputs inputs)
    {
        var parts = new (string Name, double Ratio)[]
        {
            ("Damage", Adequacy(inputs.DamagePerTurn, inputs.DamageNeededPerTurn)),
            ("Block", Adequacy(inputs.MitigationPerTurn, inputs.DamageTakenPerTurn)),
            ("Scaling", Adequacy(inputs.ScalingCards, inputs.ScalingNeeded)),
            ("Acceleration", Adequacy(inputs.AccelerationCards, inputs.AccelerationNeeded)),
        };

        // Nothing to compare against yet, in the opening turns of a run.
        if (parts.All(p => double.IsNaN(p.Ratio)))
        {
            return new DeckPowerResult(0, "", HasData: false);
        }

        var known = parts.Where(p => !double.IsNaN(p.Ratio)).ToList();

        // Harmonic mean, the standard "no better than your bottleneck" average. A geometric mean
        // was tried first and proved far too forgiving: a deck with no block at all still landed
        // mid-scale, because taking a fourth root softens a zero into something survivable-looking.
        // The harmonic mean is dominated by the smallest term, so the same deck bottoms out, which
        // is the honest answer.
        var reciprocals = known.Sum(p => 1.0 / Math.Max(p.Ratio, 0.01));
        var mean = known.Count / reciprocals;

        var weakest = known.OrderBy(p => p.Ratio).First();

        return new DeckPowerResult(ToScale(mean), weakest.Name, HasData: true);
    }

    /// <summary>
    /// Maps a mean adequacy onto the 1–10 scale. Linear on purpose: the curve is already in the
    /// harmonic mean, and bending it twice would make the number harder to reason about, not easier.
    /// </summary>
    public static double ToScale(double meanAdequacy)
    {
        var fraction = Math.Clamp(meanAdequacy / MaxAdequacy, 0, 1);
        var score = MinScore + fraction * (MaxScore - MinScore);
        return Math.Round(score, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Delivered over demanded, capped. NaN means the demand is not known yet, and that category is left
    /// out of the average rather than counted as a failure.
    /// </summary>
    private static double Adequacy(double have, double need)
    {
        if (need <= 0 || double.IsNaN(need))
        {
            return double.NaN;
        }

        return Math.Clamp(have / need, 0, MaxAdequacy);
    }

    /// <summary>
    /// How many scaling cards a deck wants by a given act. Judgement rather than measurement: unlike
    /// damage and block there is nothing in the run to compare against, so these are the weakest
    /// inputs to the score and are deliberately forgiving.
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
