namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>Broad category of a card, mirroring the game's CardType without depending on it.</summary>
public enum CardKind
{
    Other,
    Attack,
    Skill,
    Power,
    Status,
    Curse,
}

/// <summary>
/// Everything the classifier needs to know about one card, copied out of the game's model.
/// Keeping this free of game and engine types is what makes the classification logic unit-testable.
/// </summary>
public readonly record struct CardFacts(
    string ClassName,
    string DisplayName,
    CardKind Kind,
    bool TargetsAllEnemies,
    bool TargetsSelf,
    decimal Damage,
    decimal Block,
    bool GainsBlock,
    decimal Draw,
    decimal StrengthGain,
    decimal DexterityGain,
    int EnergyCost = 0,
    bool CostsX = false)
{
    /// <summary>Convenience factory for tests and for cards whose data could not be read.</summary>
    public static CardFacts Named(string className, CardKind kind = CardKind.Other) =>
        new(className, className, kind, false, false, 0m, 0m, false, 0m, 0m, 0m);
}
