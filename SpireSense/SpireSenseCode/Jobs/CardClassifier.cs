using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace SpireSense.SpireSenseCode.Jobs;

public enum ClassificationSource
{
    /// <summary>Card is in the curated job tables.</summary>
    Curated,
    /// <summary>Card is not in the tables; jobs were guessed from its data.</summary>
    Heuristic,
    /// <summary>Status, curse or other card that never counts toward a job.</summary>
    Ignored,
}

public readonly record struct Classification(IReadOnlySet<Job> Jobs, ClassificationSource Source);

/// <summary>
/// Decides which jobs a card performs: curated table first, then a heuristic guess from the card's
/// type, target and dynamic variables so cards added by game updates or other mods still count for something.
/// </summary>
public static class CardClassifier
{
    private static readonly IReadOnlySet<Job> None = new HashSet<Job>();

    public static Classification Classify(CardModel card)
    {
        if (card.Type is CardType.Status or CardType.Curse)
        {
            return new Classification(None, ClassificationSource.Ignored);
        }

        if (JobDatabase.TryGet(card.GetType().Name, out var curated))
        {
            return new Classification(curated, ClassificationSource.Curated);
        }

        return new Classification(Guess(card), ClassificationSource.Heuristic);
    }

    private static IReadOnlySet<Job> Guess(CardModel card)
    {
        var jobs = new HashSet<Job>();

        try
        {
            if (card.Type == CardType.Power)
            {
                jobs.Add(Job.Scaling);
                return jobs;
            }

            var vars = card.DynamicVars;

            if (card.Type == CardType.Attack && vars.ContainsKey("Damage") && vars["Damage"].BaseValue > 0)
            {
                jobs.Add(Job.FrontloadedDamage);
                if (card.TargetType == TargetType.AllEnemies)
                {
                    jobs.Add(Job.FrontloadedAoe);
                }
            }

            if (card.GainsBlock || (vars.ContainsKey("Block") && vars["Block"].BaseValue > 0))
            {
                jobs.Add(Job.FrontloadedBlock);
            }

            if (vars.ContainsKey("Cards") && vars["Cards"].BaseValue > 0)
            {
                jobs.Add(Job.CardDraw);
            }

            if (card.TargetType == TargetType.Self &&
                ((vars.ContainsKey("StrengthPower") && vars["StrengthPower"].BaseValue > 0) ||
                 (vars.ContainsKey("DexterityPower") && vars["DexterityPower"].BaseValue > 0)))
            {
                jobs.Add(Job.Scaling);
            }
        }
        catch (Exception ex)
        {
            SpireSenseMod.Logger.Warn($"Heuristic classification failed for {card.GetType().Name}: {ex.Message}");
        }

        return jobs;
    }
}
