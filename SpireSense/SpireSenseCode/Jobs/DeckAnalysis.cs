namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// Job counts for one deck. Immutable snapshot; compare with <see cref="Equals(DeckAnalysis?)"/> to detect changes.
/// </summary>
public sealed class DeckAnalysis : IEquatable<DeckAnalysis>
{
    public int TotalCards { get; private init; }
    public int IgnoredCards { get; private init; }
    public IReadOnlyDictionary<Job, int> Counts { get; private init; } = EmptyCounts();
    public IReadOnlyDictionary<Job, int> GuessedCounts { get; private init; } = EmptyCounts();
    public IReadOnlyList<string> UnclassifiedCardNames { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<string> GuessedCardNames { get; private init; } = Array.Empty<string>();

    /// <summary>Estimated damage per turn, averaged over one cycle through the deck.</summary>
    public double AvgCycleDamage { get; private init; }

    /// <summary>Estimated block per turn, averaged over one cycle through the deck.</summary>
    public double AvgCycleMitigation { get; private init; }

    public static readonly DeckAnalysis Empty = new();

    private static Dictionary<Job, int> EmptyCounts() => JobInfo.All.ToDictionary(j => j, _ => 0);

    /// <summary>
    /// What share of the deck does this job, as a whole-number percentage of every card in it.
    /// Curses and statuses are part of the denominator because they are part of the deck you draw
    /// from, and the header counts them too.
    /// </summary>
    public int PercentFor(Job job) => PercentOfDeck(Counts[job]);

    /// <summary>A card count as a whole-number percentage of the deck.</summary>
    public int PercentOfDeck(int count) =>
        TotalCards == 0 ? 0 : (int)Math.Round(count * 100.0 / TotalCards, MidpointRounding.AwayFromZero);

    public static DeckAnalysis Analyze(IEnumerable<CardFacts> deck)
    {
        var counts = EmptyCounts();
        var guessed = EmptyCounts();
        var unclassified = new List<string>();
        var guessedNames = new List<string>();
        int total = 0;
        int ignored = 0;
        decimal deckDamage = 0;
        decimal deckBlock = 0;
        int deckEnergy = 0;

        foreach (var card in deck)
        {
            total++;

            // Curses and statuses cannot be played, so they cost nothing and contribute nothing,
            // but they still count toward the deck size and so lengthen the cycle.
            if (card.Kind is not (CardKind.Curse or CardKind.Status))
            {
                deckDamage += card.Damage;
                deckBlock += card.Block;
                // An X-cost card consumes whatever energy is left rather than a fixed amount, so
                // costing it at zero would make the deck look far cheaper than it plays.
                deckEnergy += card.CostsX ? CycleEstimate.EnergyPerTurn : card.EnergyCost;
            }
            var result = CardClassifier.Classify(card);

            switch (result.Source)
            {
                case ClassificationSource.Ignored:
                    ignored++;
                    continue;
                case ClassificationSource.Heuristic:
                    if (result.Jobs.Count == 0)
                    {
                        unclassified.Add(card.DisplayName);
                    }
                    else
                    {
                        guessedNames.Add(card.DisplayName);
                    }
                    break;
            }

            foreach (var job in result.Jobs)
            {
                counts[job]++;
                if (result.Source == ClassificationSource.Heuristic)
                {
                    guessed[job]++;
                }
            }
        }

        return new DeckAnalysis
        {
            TotalCards = total,
            IgnoredCards = ignored,
            Counts = counts,
            GuessedCounts = guessed,
            UnclassifiedCardNames = unclassified,
            GuessedCardNames = guessedNames,
            AvgCycleDamage = CycleEstimate.PerTurn((double)deckDamage, total, deckEnergy),
            AvgCycleMitigation = CycleEstimate.PerTurn((double)deckBlock, total, deckEnergy),
        };
    }

    public bool Equals(DeckAnalysis? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (TotalCards != other.TotalCards || IgnoredCards != other.IgnoredCards) return false;
        foreach (var job in JobInfo.All)
        {
            if (Counts[job] != other.Counts[job] || GuessedCounts[job] != other.GuessedCounts[job]) return false;
        }
        if (Math.Abs(AvgCycleDamage - other.AvgCycleDamage) > 0.05
            || Math.Abs(AvgCycleMitigation - other.AvgCycleMitigation) > 0.05)
        {
            return false;
        }
        return UnclassifiedCardNames.SequenceEqual(other.UnclassifiedCardNames)
            && GuessedCardNames.SequenceEqual(other.GuessedCardNames);
    }

    public override bool Equals(object? obj) => Equals(obj as DeckAnalysis);

    public override int GetHashCode() =>
        HashCode.Combine(TotalCards, IgnoredCards, Counts[Job.FrontloadedDamage], Counts[Job.Scaling], Counts[Job.CardDraw]);
}
