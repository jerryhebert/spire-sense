using System.Text.Json;
using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

/// <summary>
/// Guards the shipped classification data itself. The tables are large and hand-curated, so these
/// tests are what stop a bad edit from silently shipping.
/// </summary>
public class CategoryTableDataTests
{
    public CategoryTableDataTests() => TestData.LoadRealTables();

    [Fact]
    public void LoadsEveryPoolTable()
    {
        Assert.Equal(6, CategoryDatabase.PoolCount);
    }

    [Fact]
    public void CoversTheWholeCardPool()
    {
        // Ironclad 90 + Silent 91 + Defect 91 + Necrobinder 91 + Regent 91 + Colorless 65.
        Assert.Equal(519, CategoryDatabase.CardCount);
    }

    [Fact]
    public void NoCardAppearsInTwoTables()
    {
        // A duplicate would silently discard one table's verdict for that card.
        Assert.Empty(CategoryDatabase.Duplicates);
    }

    [Fact]
    public void AreaDamageDoesNotRequireFrontloadedDamage()
    {
        // Area damage is its own category, so powers that hit every enemy over time can carry it alone.
        // If nothing does, the tables have slipped back to treating it as a sub-category.
        var aoeWithoutFrontloaded = CategoryDatabase.All
            .Count(e => e.Value.Contains(Category.Aoe) && !e.Value.Contains(Category.FrontloadedDamage));

        Assert.True(aoeWithoutFrontloaded > 0,
            "No card has area damage without frontloaded damage, which suggests the old subset rule crept back.");
    }

    [Fact]
    public void EveryCategoryNameInTheRawFilesIsRecognized()
    {
        // CategoryDatabase skips unknown category names with a warning, so a typo would not fail loading.
        // Reading the raw JSON catches it instead.
        var valid = Enum.GetNames<Category>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();

        foreach (var (resource, root) in ReadRawTables())
        {
            foreach (var card in root.GetProperty("cards").EnumerateObject())
            {
                foreach (var category in card.Value.GetProperty("categories").EnumerateArray())
                {
                    var name = category.GetString();
                    if (name == null || !valid.Contains(name))
                    {
                        bad.Add($"{resource}:{card.Name}:{name}");
                    }
                }
            }
        }

        Assert.Empty(bad);
    }

    [Fact]
    public void NoNoteDescribesAreaDamageWithoutTheAreaDamageTag()
    {
        // This is the bug that prompted the rewrite, caught mechanically. Every wrong entry in the
        // first pass had already written the disqualifying fact into its own note: Inferno read
        // "6 dmg to all enemies" while tagged Scaling only. If a note says a card damages every
        // enemy, the tags have to agree.
        var offenders = new List<string>();

        foreach (var (resource, root) in ReadRawTables())
        {
            foreach (var card in root.GetProperty("cards").EnumerateObject())
            {
                var note = card.Value.GetProperty("note").GetString() ?? "";
                if (!DescribesAreaDamage(note))
                {
                    continue;
                }

                var categories = card.Value.GetProperty("categories").EnumerateArray().Select(j => j.GetString()).ToList();
                if (!categories.Contains(nameof(Category.Aoe)))
                {
                    offenders.Add($"{resource}:{card.Name}: \"{note}\" tagged [{string.Join(", ", categories)}]");
                }
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Reads a note as claiming damage to every enemy. Deliberately conservative: a debuff applied
    /// to all enemies is not area damage, and multi-hit random targeting does not reliably spread,
    /// so both are excluded rather than reported as false alarms.
    /// </summary>
    private static bool DescribesAreaDamage(string note)
    {
        var text = note.ToLowerInvariant();

        var hitsEveryone = text.Contains("all enemies") || text.Contains("every enemy")
            || text.Contains("every other enemy") || text.Contains("to all")
            || text.Contains("all other enemies");

        var doesDamage = text.Contains("dmg") || text.Contains("damage") || text.Contains("poison");

        var randomlyTargeted = text.Contains("random");

        // A note that states outright the card deals no damage is the author exempting it, which
        // is better than a silent exception list: the reason sits next to the card.
        var deniesDamage = text.Contains("no dmg") || text.Contains("no damage");

        return hitsEveryone && doesDamage && !randomlyTargeted && !deniesDamage;
    }

    [Fact]
    public void EveryCardHasAnExplanatoryNote()
    {
        var missing = new List<string>();

        foreach (var (resource, root) in ReadRawTables())
        {
            foreach (var card in root.GetProperty("cards").EnumerateObject())
            {
                if (!card.Value.TryGetProperty("note", out var note) || string.IsNullOrWhiteSpace(note.GetString()))
                {
                    missing.Add($"{resource}:{card.Name}");
                }
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void MostCardsHaveAtLeastOneCategory()
    {
        // Empty is legal for pure utility cards, but a large jump would mean the data regressed.
        var empty = CategoryDatabase.All.Count(e => e.Value.Count == 0);
        Assert.InRange(empty, 0, 60);
    }

    [Theory]
    // Spot checks across every pool, including multi-category cards.
    [InlineData("StrikeIronclad", Category.FrontloadedDamage)]
    [InlineData("DefendIronclad", Category.FrontloadedBlock)]
    [InlineData("DemonForm", Category.ScalingDamage)]
    [InlineData("Thunderclap", Category.Aoe)]
    [InlineData("BattleTrance", Category.Acceleration)]
    [InlineData("ShrugItOff", Category.FrontloadedBlock)]
    [InlineData("ShrugItOff", Category.Acceleration)]
    [InlineData("NoxiousFumes", Category.ScalingDamage)]
    [InlineData("Defragment", Category.ScalingDamage)]
    public void KnownCardsHaveTheExpectedCategory(string cardClassName, Category expected)
    {
        Assert.True(CategoryDatabase.TryGet(cardClassName, out var categories), $"{cardClassName} is missing from the tables");
        Assert.Contains(expected, categories);
    }

    [Fact]
    public void ThunderclapCountsAsBothDamageAndAoe()
    {
        Assert.True(CategoryDatabase.TryGet("Thunderclap", out var categories));
        Assert.Contains(Category.FrontloadedDamage, categories);
        Assert.Contains(Category.Aoe, categories);
    }

    [Theory]
    // Regressions from the first pass, which assigned each card one headline category and stopped.
    // Powers that damage every enemy were filed as Scaling only; effects that grow within a turn
    // were filed by their immediate effect only.
    [InlineData("Inferno", Category.Aoe)]
    [InlineData("Inferno", Category.ScalingDamage)]
    [InlineData("Rage", Category.FrontloadedBlock)]
    [InlineData("Rage", Category.ScalingBlock)]
    [InlineData("NoxiousFumes", Category.Aoe)]
    [InlineData("Panache", Category.Aoe)]
    [InlineData("Hailstorm", Category.Aoe)]
    [InlineData("BlackHole", Category.Aoe)]
    [InlineData("TheBomb", Category.Aoe)]
    public void CardsMissedByTheFirstPassAreClassifiedNow(string cardClassName, Category expected)
    {
        Assert.True(CategoryDatabase.TryGet(cardClassName, out var categories), $"{cardClassName} is missing from the tables");
        Assert.Contains(expected, categories);
    }

    private static IEnumerable<(string Resource, JsonElement Root)> ReadRawTables()
    {
        var assembly = TestData.TablesAssembly;
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(CategoryDatabase.ResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);
            yield return (name, doc.RootElement.Clone());
        }
    }
}
