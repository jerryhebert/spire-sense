using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpireSense.SpireSenseCode.Categories;

/// <summary>
/// Hand-curated card-to-category table, loaded from the categories.*.json files embedded in this assembly.
/// Keys are the card's C# class name (e.g. "PommelStrike"), which is stable across languages.
/// </summary>
public static class CategoryDatabase
{
    /// <summary>Prefix of the embedded resource names holding the category tables.</summary>
    public const string ResourcePrefix = "SpireSense.Data.categories.";

    private sealed class PoolFile
    {
        [JsonPropertyName("pool")] public string? Pool { get; set; }
        [JsonPropertyName("cards")] public Dictionary<string, CardEntry>? Cards { get; set; }
    }

    public sealed class CardEntry
    {
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    private static readonly Dictionary<string, IReadOnlySet<Category>> ByClassName = new(StringComparer.Ordinal);
    private static readonly List<string> DuplicateKeys = new();

    public static int CardCount => ByClassName.Count;
    public static int PoolCount { get; private set; }

    /// <summary>Card class names that appeared in more than one pool file. Should always be empty.</summary>
    public static IReadOnlyList<string> Duplicates => DuplicateKeys;

    public static IReadOnlyDictionary<string, IReadOnlySet<Category>> All => ByClassName;

    /// <summary>Loads every category table embedded in the given assembly, replacing anything loaded before.</summary>
    public static void Load(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        ByClassName.Clear();
        DuplicateKeys.Clear();
        PoolCount = 0;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    continue;
                }

                var file = JsonSerializer.Deserialize<PoolFile>(stream, options);
                if (file?.Cards == null)
                {
                    ModLog.Warn($"Category table {resourceName} has no cards");
                    continue;
                }

                PoolCount++;
                foreach (var pair in file.Cards)
                {
                    if (ByClassName.ContainsKey(pair.Key))
                    {
                        // Two tables claiming the same card would silently drop one verdict.
                        DuplicateKeys.Add(pair.Key);
                        ModLog.Warn($"Card {pair.Key} appears in more than one category table; keeping the first");
                        continue;
                    }

                    ByClassName[pair.Key] = ParseCategories(pair.Key, pair.Value, resourceName);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error($"Failed to load category table {resourceName}: {ex}");
            }
        }
    }

    private static IReadOnlySet<Category> ParseCategories(string className, CardEntry entry, string resourceName)
    {
        var categories = new HashSet<Category>();
        foreach (var categoryName in entry.Categories ?? new List<string>())
        {
            if (CategoryInfo.TryParse(categoryName, out var category))
            {
                categories.Add(category);
            }
            else
            {
                ModLog.Warn($"Unknown category '{categoryName}' on card {className} in {resourceName}");
            }
        }

        return categories;
    }

    public static bool TryGet(string cardClassName, out IReadOnlySet<Category> categories) =>
        ByClassName.TryGetValue(cardClassName, out categories!);

    public static bool Has(string cardClassName) => ByClassName.ContainsKey(cardClassName);
}
