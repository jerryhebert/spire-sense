namespace SpireSense.SpireSenseCode.Categories;

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

    /// <summary>
    /// Damage enemies aimed at you, blocked and unblocked alike. Deliberately not the damage that
    /// got through: what block has to keep up with is what is being thrown, and measuring what
    /// landed makes the requirement fall as block succeeds. A deck that blocks everything would
    /// have reported no incoming damage at all, and dropped out of the score for being too good.
    /// </summary>
    public double TotalIncomingDamage { get; private set; }

    /// <summary>Distinct combat turns seen, which is what the totals are averaged over.</summary>
    public int TurnsObserved { get; private set; }

    private string? _lastTurnKey;
    private string? _lastFightKey;
    private FightKind _currentFight = FightKind.Normal;

    /// <summary>
    /// Health actually lost, per kind of fight. This is the attrition figure, so unlike
    /// <see cref="TotalIncomingDamage"/> it is what got through block: blocked damage costs you
    /// nothing and does not belong in what a fight costs.
    /// </summary>
    private readonly Dictionary<FightKind, double> _hpLost = NewTally<double>();

    private readonly Dictionary<FightKind, int> _fightsSeen = NewTally<int>();

    private static Dictionary<FightKind, T> NewTally<T>() where T : struct =>
        FightKindInfo.All.ToDictionary(k => k, _ => default(T));

    /// <summary>Fights of this kind seen so far this run.</summary>
    public int FightsSeen(FightKind kind) => _fightsSeen[kind];

    /// <summary>Total health lost in fights of this kind.</summary>
    public double HpLost(FightKind kind) => _hpLost[kind];

    /// <summary>
    /// What a fight of this kind has cost you on average, or NaN if you have not had one yet.
    /// NaN rather than zero, because "no elite yet" and "elites are free" are opposite facts.
    /// </summary>
    public double HpLostPerFight(FightKind kind) =>
        _fightsSeen[kind] == 0 ? double.NaN : _hpLost[kind] / _fightsSeen[kind];

    /// <summary>
    /// Registers the combat being fought and what kind it is. Called every frame while in combat,
    /// so the fight is counted once and its kind stays current even if the room is readable only
    /// after the fight has started.
    /// </summary>
    public void NoteFight(string fightKey, FightKind kind)
    {
        _currentFight = kind;

        if (fightKey != _lastFightKey)
        {
            _lastFightKey = fightKey;
            _fightsSeen[kind]++;
        }
    }

    /// <summary>Health lost, charged to the fight in progress.</summary>
    public void AddHpLost(double amount)
    {
        if (amount > 0)
        {
            _hpLost[_currentFight] += amount;
        }
    }

    /// <summary>
    /// Size of the hand dealt at the last turn start, as the game computed it, so relics and powers
    /// that change it are reflected. Used for cycle length, which is about how fast the deck is
    /// seen, not how many cards you end up holding.
    /// </summary>
    public double HandDrawSize { get; private set; } = CycleEstimate.BaseCardsDrawnPerTurn;

    /// <summary>Every card drawn, turn-start hands and draw effects alike.</summary>
    public double TotalCardsDrawn { get; private set; }

    /// <summary>
    /// Cards drawn in an average turn. Unlike the hand size this includes draw from cards and
    /// powers, so it is the number that shows whether a draw engine is actually working.
    /// </summary>
    public double CardsDrawnPerTurn => TurnsObserved == 0 ? 0 : TotalCardsDrawn / TurnsObserved;

    public void NoteHandDraw(double cards)
    {
        if (cards > 0)
        {
            HandDrawSize = cards;
        }
    }

    public void AddCardsDrawn(int cards)
    {
        if (cards > 0)
        {
            TotalCardsDrawn += cards;
        }
    }

    /// <summary>False until a turn has been played, while the static estimate stands in.</summary>
    public bool HasData => TurnsObserved > 0;

    public double DamagePerTurn => TurnsObserved == 0 ? 0 : TotalDamage / TurnsObserved;

    public double MitigationPerTurn => TurnsObserved == 0 ? 0 : TotalMitigation / TurnsObserved;

    public double IncomingDamagePerTurn => TurnsObserved == 0 ? 0 : TotalIncomingDamage / TurnsObserved;

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

    public void AddIncomingDamage(double amount)
    {
        if (amount > 0)
        {
            TotalIncomingDamage += amount;
        }
    }

    /// <summary>Starts a fresh set of figures. Called when a new run begins.</summary>
    public static void StartNewRun() => Current = new RunStats();

    /// <summary>Test seam.</summary>
    public static void ReplaceCurrent(RunStats stats) => Current = stats;
}
