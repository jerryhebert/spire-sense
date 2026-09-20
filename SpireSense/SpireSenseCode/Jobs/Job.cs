namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The deckbuilding "jobs" from the "Solving the Spire with Jobs" framework.
/// A card may perform several jobs at once; see Data/CLASSIFICATION.md.
/// </summary>
public enum Job
{
    FrontloadedDamage,

    /// <summary>
    /// Damages every enemy, whenever that damage lands. Independent of <see cref="FrontloadedDamage"/>
    /// on purpose: a power that hits all enemies every turn answers "can this deck handle three
    /// enemies" even though it does nothing the turn it is played.
    /// </summary>
    Aoe,

    FrontloadedBlock,
    Scaling,
    CardDraw,
}

public static class JobInfo
{
    public static readonly Job[] All =
    {
        Job.FrontloadedDamage,
        Job.Aoe,
        Job.FrontloadedBlock,
        Job.Scaling,
        Job.CardDraw,
    };

    public static string DisplayName(Job job) => job switch
    {
        Job.FrontloadedDamage => "Frontloaded Damage",
        Job.Aoe => "Area Damage",
        Job.FrontloadedBlock => "Frontloaded Block",
        Job.Scaling => "Scaling",
        Job.CardDraw => "Card Draw / Manip.",
        _ => job.ToString(),
    };

    /// <summary>
    /// Parses a job name, accepting the retired "FrontloadedAoe" so that override files written
    /// before area damage became its own job keep working.
    /// </summary>
    public static bool TryParse(string name, out Job job)
    {
        if (string.Equals(name, "FrontloadedAoe", StringComparison.OrdinalIgnoreCase))
        {
            job = Job.Aoe;
            return true;
        }

        return Enum.TryParse(name, ignoreCase: true, out job);
    }
}
