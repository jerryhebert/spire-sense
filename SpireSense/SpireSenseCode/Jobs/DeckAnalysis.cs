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

    public static readonly DeckAnalysis Empty = new();

    private static Dictionary<Job, int> EmptyCounts() => JobInfo.All.ToDictionary(j => j, _ => 0);

    public static DeckAnalysis Analyze(IEnumerable<CardFacts> deck)
    {
        var counts = EmptyCounts();
        var guessed = EmptyCounts();
        var unclassified = new List<string>();
        var guessedNames = new List<string>();
        int total = 0;
        int ignored = 0;

        foreach (var card in deck)
        {
            total++;
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
        return UnclassifiedCardNames.SequenceEqual(other.UnclassifiedCardNames)
            && GuessedCardNames.SequenceEqual(other.GuessedCardNames);
    }

    public override bool Equals(object? obj) => Equals(obj as DeckAnalysis);

    public override int GetHashCode() =>
        HashCode.Combine(TotalCards, IgnoredCards, Counts[Job.FrontloadedDamage], Counts[Job.Scaling], Counts[Job.CardDraw]);
}
