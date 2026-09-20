using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// Hand-curated card-to-job table, loaded from the jobs.*.json files embedded in this assembly.
/// Keys are the card's C# class name (e.g. "PommelStrike"), which is stable across languages.
/// </summary>
public static class JobDatabase
{
    /// <summary>Prefix of the embedded resource names holding the job tables.</summary>
    public const string ResourcePrefix = "SpireSense.Data.jobs.";

    private sealed class PoolFile
    {
        [JsonPropertyName("pool")] public string? Pool { get; set; }
        [JsonPropertyName("cards")] public Dictionary<string, CardEntry>? Cards { get; set; }
    }

    public sealed class CardEntry
    {
        [JsonPropertyName("jobs")] public List<string>? Jobs { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    private static readonly Dictionary<string, IReadOnlySet<Job>> ByClassName = new(StringComparer.Ordinal);
    private static readonly List<string> DuplicateKeys = new();

    public static int CardCount => ByClassName.Count;
    public static int PoolCount { get; private set; }

    /// <summary>Card class names that appeared in more than one pool file. Should always be empty.</summary>
    public static IReadOnlyList<string> Duplicates => DuplicateKeys;

    public static IReadOnlyDictionary<string, IReadOnlySet<Job>> All => ByClassName;

    /// <summary>Loads every job table embedded in the given assembly, replacing anything loaded before.</summary>
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
                    ModLog.Warn($"Job table {resourceName} has no cards");
                    continue;
                }

                PoolCount++;
                foreach (var pair in file.Cards)
                {
                    if (ByClassName.ContainsKey(pair.Key))
                    {
                        // Two tables claiming the same card would silently drop one verdict.
                        DuplicateKeys.Add(pair.Key);
                        ModLog.Warn($"Card {pair.Key} appears in more than one job table; keeping the first");
                        continue;
                    }

                    ByClassName[pair.Key] = ParseJobs(pair.Key, pair.Value, resourceName);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error($"Failed to load job table {resourceName}: {ex}");
            }
        }
    }

    private static IReadOnlySet<Job> ParseJobs(string className, CardEntry entry, string resourceName)
    {
        var jobs = new HashSet<Job>();
        foreach (var jobName in entry.Jobs ?? new List<string>())
        {
            if (Enum.TryParse<Job>(jobName, ignoreCase: true, out var job))
            {
                jobs.Add(job);
            }
            else
            {
                ModLog.Warn($"Unknown job '{jobName}' on card {className} in {resourceName}");
            }
        }

        // AoE is defined as a subset of frontloaded damage; enforce that regardless of the data.
        if (jobs.Contains(Job.FrontloadedAoe))
        {
            jobs.Add(Job.FrontloadedDamage);
        }

        return jobs;
    }

    public static bool TryGet(string cardClassName, out IReadOnlySet<Job> jobs) =>
        ByClassName.TryGetValue(cardClassName, out jobs!);

    public static bool Has(string cardClassName) => ByClassName.ContainsKey(cardClassName);
}
