namespace SpireSense.SpireSenseCode.Categories;

/// <summary>
/// What a card does for a deck. A card may belong to several categories at once; the definitions
/// live in Data/CLASSIFICATION.md.
/// </summary>
public enum Category
{
    /// <summary>Damage the turn it is played, no setup.</summary>
    FrontloadedDamage,

    /// <summary>Damage that grows or repeats: poison, powers, Strength, orb engines.</summary>
    ScalingDamage,

    /// <summary>
    /// Damages every enemy, whenever that damage lands. Independent of the two above on purpose: a
    /// power that hits all enemies every turn answers "can this deck handle three enemies" even
    /// though it does nothing the turn it is played.
    /// </summary>
    Aoe,

    /// <summary>Prevents damage the turn it is played.</summary>
    FrontloadedBlock,

    /// <summary>Defence that grows or repeats: Plating, Dexterity, Barricade, block per play.</summary>
    ScalingBlock,

    /// <summary>Draw, scry, tutor, retain, thinning, energy, cost reduction, extra plays.</summary>
    Acceleration,
}

public static class CategoryInfo
{
    public static readonly Category[] All =
    {
        Category.FrontloadedDamage,
        Category.ScalingDamage,
        Category.Aoe,
        Category.FrontloadedBlock,
        Category.ScalingBlock,
        Category.Acceleration,
    };

    /// <summary>
    /// Abbreviated on purpose: the panel is narrow and these sit beside numbers that need to line
    /// up, so "FL." and "Sc." beat spelling out frontloaded and scaling on every row.
    /// </summary>
    public static string DisplayName(Category category) => category switch
    {
        Category.FrontloadedDamage => "FL. Damage",
        Category.ScalingDamage => "Sc. Damage",
        Category.Aoe => "AOE",
        Category.FrontloadedBlock => "FL. Block",
        Category.ScalingBlock => "Sc. Block",
        Category.Acceleration => "Acceleration",
        _ => category.ToString(),
    };

    /// <summary>
    /// The heading a category sits under on the panel, or null for one that stands on its own.
    /// Grouping lets the rows inside a group drop the repeated half of their name.
    /// </summary>
    public static string? Group(Category category) => category switch
    {
        Category.FrontloadedDamage or Category.ScalingDamage or Category.Aoe => "Damage",
        Category.FrontloadedBlock or Category.ScalingBlock => "Block",
        _ => null,
    };

    /// <summary>
    /// What a category is called underneath its group heading, where "Damage" or "Block" is already
    /// established and repeating it would be noise. Meaningless without the heading above it, so
    /// anywhere a category appears on its own wants <see cref="DisplayName"/> instead.
    /// </summary>
    public static string ShortName(Category category) => category switch
    {
        Category.FrontloadedDamage or Category.FrontloadedBlock => "Frontloaded",
        Category.ScalingDamage or Category.ScalingBlock => "Scaling",
        Category.Aoe => "AOE",
        Category.Acceleration => "Acceleration",
        _ => category.ToString(),
    };

    /// <summary>Spelled out, for places with room to explain what an abbreviation stands for.</summary>
    public static string LongName(Category category) => category switch
    {
        Category.FrontloadedDamage => "Frontloaded damage",
        Category.ScalingDamage => "Scaling damage",
        Category.Aoe => "Area damage",
        Category.FrontloadedBlock => "Frontloaded block",
        Category.ScalingBlock => "Scaling block",
        Category.Acceleration => "Acceleration",
        _ => category.ToString(),
    };

    /// <summary>
    /// Parses a category name, accepting names retired in earlier versions so that an override file
    /// written before a rename keeps working.
    /// </summary>
    public static bool TryParse(string name, out Category category)
    {
        switch (name.ToLowerInvariant())
        {
            // "FrontloadedAoe" predates area damage becoming independent.
            case "frontloadedaoe":
                category = Category.Aoe;
                return true;

            // "Scaling" predates the damage and block split. Damage is the commoner intent, and a
            // user who meant block can retag the card in one click.
            case "scaling":
                category = Category.ScalingDamage;
                return true;

            // "CardDraw" predates the wider Acceleration category that replaced it.
            case "carddraw":
                category = Category.Acceleration;
                return true;
        }

        return Enum.TryParse(name, ignoreCase: true, out category);
    }
}
