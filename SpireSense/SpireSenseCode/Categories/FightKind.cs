namespace SpireSense.SpireSenseCode.Categories;

/// <summary>
/// The kinds of combat worth telling apart when measuring what a fight costs you.
///
/// Its own enum rather than the game's RoomType so that everything in this folder stays free of
/// game types and can be unit-tested; <c>Game/RunAccess</c> maps one to the other.
/// </summary>
public enum FightKind
{
    /// <summary>An ordinary encounter.</summary>
    Normal,
    Elite,
    Boss,
}

public static class FightKindInfo
{
    public static readonly FightKind[] All = { FightKind.Normal, FightKind.Elite, FightKind.Boss };

    public static string DisplayName(FightKind kind) => kind switch
    {
        FightKind.Normal => "Normal",
        FightKind.Elite => "Elite",
        FightKind.Boss => "Boss",
        _ => kind.ToString(),
    };

    /// <summary>How the kind reads in a sentence, e.g. "Elites cost 24 HP".</summary>
    public static string Plural(FightKind kind) => kind switch
    {
        FightKind.Normal => "Fights",
        FightKind.Elite => "Elites",
        FightKind.Boss => "Bosses",
        _ => kind.ToString(),
    };
}
