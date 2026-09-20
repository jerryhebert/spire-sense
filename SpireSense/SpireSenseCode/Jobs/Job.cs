namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// The deckbuilding "jobs" from the "Solving the Spire with Jobs" framework.
/// A card may perform several jobs at once.
/// </summary>
public enum Job
{
    FrontloadedDamage,
    FrontloadedAoe,
    FrontloadedBlock,
    Scaling,
    CardDraw,
}

public static class JobInfo
{
    public static readonly Job[] All =
    {
        Job.FrontloadedDamage,
        Job.FrontloadedAoe,
        Job.FrontloadedBlock,
        Job.Scaling,
        Job.CardDraw,
    };

    public static string DisplayName(Job job) => job switch
    {
        Job.FrontloadedDamage => "Frontloaded Damage",
        Job.FrontloadedAoe => "  of which AoE",
        Job.FrontloadedBlock => "Frontloaded Block",
        Job.Scaling => "Scaling",
        Job.CardDraw => "Card Draw / Manip.",
        _ => job.ToString(),
    };
}
