namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// What the deck actually did this run, as opposed to what <see cref="CycleEstimate"/> predicts it
/// will do. Measured figures beat the static model on everything the card data cannot see:
/// Strength, relics, powers, orbs, multi-hit attacks, and any card the classifier reads wrongly.
///
/// Accumulated over the whole run and reset when a new one starts, so the numbers get steadier as
/// the run goes on rather than jumping about after every fight.
/// </summary>
public sealed class RunStats
{
    /// <summary>Stats for the run in progress.</summary>
    public static RunStats Current { get; private set; } = new();

    /// <summary>Damage dealt to enemies, excluding overkill.</summary>
    public double TotalDamage { get; private set; }

    /// <summary>Block gained.</summary>
    public double TotalMitigation { get; private set; }

    /// <summary>Distinct combat turns seen, which is what the totals are averaged over.</summary>
    public int TurnsObserved { get; private set; }

    private string? _lastTurnKey;

    /// <summary>False until a turn has been played, while the static estimate stands in.</summary>
    public bool HasData => TurnsObserved > 0;

    public double DamagePerTurn => TurnsObserved == 0 ? 0 : TotalDamage / TurnsObserved;

    public double MitigationPerTurn => TurnsObserved == 0 ? 0 : TotalMitigation / TurnsObserved;

    /// <summary>
    /// Registers the turn currently being played. Called every frame with a key identifying the
    /// combat and turn number, so a turn where you neither dealt damage nor gained block still
    /// counts: those turns are real and dropping them would flatter the averages.
    /// </summary>
    public void NoteTurn(string turnKey)
    {
        if (turnKey != _lastTurnKey)
        {
            _lastTurnKey = turnKey;
            TurnsObserved++;
        }
    }

    public void AddDamage(double amount)
    {
        if (amount > 0)
        {
            TotalDamage += amount;
        }
    }

    public void AddMitigation(double amount)
    {
        if (amount > 0)
        {
            TotalMitigation += amount;
        }
    }

    /// <summary>Starts a fresh set of figures. Called when a new run begins.</summary>
    public static void StartNewRun() => Current = new RunStats();

    /// <summary>Test seam.</summary>
    public static void ReplaceCurrent(RunStats stats) => Current = stats;
}
