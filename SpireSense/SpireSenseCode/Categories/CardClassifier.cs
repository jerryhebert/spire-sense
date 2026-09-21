namespace SpireSense.SpireSenseCode.Categories;

public enum ClassificationSource
{
    /// <summary>You reclassified this card yourself; beats the shipped tables.</summary>
    Override,
    /// <summary>Card is in the curated category tables.</summary>
    Curated,
    /// <summary>Card is not in the tables; categories were guessed from its data.</summary>
    Heuristic,
    /// <summary>Status, curse or other card that never counts toward a category.</summary>
    Ignored,
}

public readonly record struct Classification(IReadOnlySet<Category> Categories, ClassificationSource Source);

/// <summary>
/// Decides which categories a card performs, in priority order: your own overrides, then the curated
/// table, then a heuristic guess from the card's kind, target and values so cards added by game
/// updates or other mods still count for something.
/// </summary>
public static class CardClassifier
{
    private static readonly IReadOnlySet<Category> NoCategories = new HashSet<Category>();

    public static Classification Classify(CardFacts card)
    {
        if (card.Kind is CardKind.Status or CardKind.Curse)
        {
            return new Classification(NoCategories, ClassificationSource.Ignored);
        }

        // Your own decisions win over the shipped tables.
        if (CategoryOverrides.TryGet(card.ClassName, out var overridden))
        {
            return new Classification(overridden, ClassificationSource.Override);
        }

        if (CategoryDatabase.TryGet(card.ClassName, out var curated))
        {
            return new Classification(curated, ClassificationSource.Curated);
        }

        return new Classification(Guess(card), ClassificationSource.Heuristic);
    }

    private static IReadOnlySet<Category> Guess(CardFacts card)
    {
        var categories = new HashSet<Category>();

        // Powers pay off over the course of a fight, which is the definition of scaling. Which
        // kind of scaling is not knowable from the card's shape, so guess damage: it is the commoner
        // case, and a curated entry or your own override replaces the guess outright.
        if (card.Kind == CardKind.Power)
        {
            categories.Add(Category.ScalingDamage);
            return categories;
        }

        if (card.Kind == CardKind.Attack && card.Damage > 0)
        {
            categories.Add(Category.FrontloadedDamage);
            if (card.TargetsAllEnemies)
            {
                categories.Add(Category.Aoe);
            }
        }

        if (card.GainsBlock || card.Block > 0)
        {
            categories.Add(Category.FrontloadedBlock);
        }

        if (card.Draw > 0)
        {
            categories.Add(Category.Acceleration);
        }

        // Permanent Strength/Dexterity on yourself grows every later card; on an enemy it is a
        // debuff. Strength grows attacks, Dexterity grows block, so they scale different halves.
        if (card.TargetsSelf && card.StrengthGain > 0)
        {
            categories.Add(Category.ScalingDamage);
        }

        if (card.TargetsSelf && card.DexterityGain > 0)
        {
            categories.Add(Category.ScalingBlock);
        }

        return categories;
    }
}
