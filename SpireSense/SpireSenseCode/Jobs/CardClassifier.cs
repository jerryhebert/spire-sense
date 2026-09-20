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
/// kind, target and values so cards added by game updates or other mods still count for something.
/// </summary>
public static class CardClassifier
{
    private static readonly IReadOnlySet<Job> NoJobs = new HashSet<Job>();

    public static Classification Classify(CardFacts card)
    {
        if (card.Kind is CardKind.Status or CardKind.Curse)
        {
            return new Classification(NoJobs, ClassificationSource.Ignored);
        }

        if (JobDatabase.TryGet(card.ClassName, out var curated))
        {
            return new Classification(curated, ClassificationSource.Curated);
        }

        return new Classification(Guess(card), ClassificationSource.Heuristic);
    }

    private static IReadOnlySet<Job> Guess(CardFacts card)
    {
        var jobs = new HashSet<Job>();

        // Powers pay off over the course of a fight, which is the definition of scaling.
        if (card.Kind == CardKind.Power)
        {
            jobs.Add(Job.Scaling);
            return jobs;
        }

        if (card.Kind == CardKind.Attack && card.Damage > 0)
        {
            jobs.Add(Job.FrontloadedDamage);
            if (card.TargetsAllEnemies)
            {
                jobs.Add(Job.FrontloadedAoe);
            }
        }

        if (card.GainsBlock || card.Block > 0)
        {
            jobs.Add(Job.FrontloadedBlock);
        }

        if (card.Draw > 0)
        {
            jobs.Add(Job.CardDraw);
        }

        // Permanent Strength/Dexterity on yourself grows every later card; on an enemy it is a debuff.
        if (card.TargetsSelf && (card.StrengthGain > 0 || card.DexterityGain > 0))
        {
            jobs.Add(Job.Scaling);
        }

        return jobs;
    }
}
