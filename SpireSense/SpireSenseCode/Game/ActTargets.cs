using MegaCrit.Sts2.Core.Models;
using SpireSense.SpireSenseCode.Categories;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Reads what the current act actually demands, rather than guessing it.
///
/// Every act lists its elite encounters and every monster declares its starting health range, so
/// the damage a deck needs is computable from the game's own numbers and moves with the act by
/// itself. That matters because eight damage a turn is fine on floor three and fatal on floor forty.
/// </summary>
public static class ActTargets
{
    private static object? _cachedRun;
    private static int _cachedActIndex = -1;
    private static double _cachedEliteHp;
    private static bool _warned;

    /// <summary>
    /// Mean starting health of an elite in the current act, or NaN if it cannot be read. Cached per
    /// act, since it only changes when the act does and walking the encounter tables is not free.
    /// </summary>
    public static double AverageEliteHp
    {
        get
        {
            var run = RunAccess.CurrentRun;
            if (run == null)
            {
                return double.NaN;
            }

            try
            {
                // Keyed on the run as well as the act. Keyed on the act alone, a new run's act 1
                // was served the previous run's cached figure, since both are act index 0.
                if (ReferenceEquals(run, _cachedRun) && run.CurrentActIndex == _cachedActIndex)
                {
                    return _cachedEliteHp;
                }

                var healths = run.Act.AllEliteEncounters
                    .SelectMany(e => e.AllPossibleMonsters)
                    .Select(m => (m.MinInitialHp + m.MaxInitialHp) / 2.0)
                    .ToList();

                _cachedRun = run;
                _cachedActIndex = run.CurrentActIndex;
                _cachedEliteHp = healths.Count > 0 ? healths.Average() : double.NaN;
                return _cachedEliteHp;
            }
            catch (Exception ex)
            {
                if (!_warned)
                {
                    _warned = true;
                    ModLog.Warn($"Could not read elite health for this act: {ex.Message}");
                }
                return double.NaN;
            }
        }
    }

    /// <summary>One-based act number, as a player would say it.</summary>
    public static int ActNumber => (RunAccess.CurrentRun?.CurrentActIndex ?? 0) + 1;

    /// <summary>Assembles everything the advice needs from the run and the measurements so far.</summary>
    public static DeckAdviceInputs BuildInputs(DeckAnalysis analysis, RunStats stats)
    {
        var act = ActNumber;

        return new DeckAdviceInputs(
            // NaN, not zero, before a turn has been played. Zero would read as a deck that deals no
            // damage rather than one nobody has watched yet, and the score would open at its floor.
            DamagePerTurn: stats.HasData ? stats.DamagePerTurn : double.NaN,
            DamageNeededPerTurn: DeckAdvice.DamageNeededForElite(AverageEliteHp),
            MitigationPerTurn: stats.HasData ? stats.MitigationPerTurn : double.NaN,
            IncomingDamagePerTurn: stats.IncomingDamagePerTurn,
            // Either kind of scaling answers the same question — does this deck still grow in a
            // long fight — so they are counted together rather than judged as two separate gaps.
            // A count of cards, not of tags: nine cards carry both scaling categories, and adding
            // the two counts let each of them fill a third of the requirement twice over.
            ScalingCards: analysis.ScalingCards,
            ScalingNeeded: DeckAdvice.ScalingNeededForAct(act),
            AccelerationCards: analysis.Counts[Category.Acceleration],
            AccelerationNeeded: DeckAdvice.AccelerationNeededForAct(act));
    }
}
