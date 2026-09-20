using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// Your own classification decisions, stored outside the mod so they survive updates and take
/// priority over the shipped tables. Keyed by card class name, like the tables themselves.
/// Plain file IO rather than Godot's FileAccess, so this is unit-testable.
/// </summary>
public static class JobOverrides
{
    private sealed class OverrideFile
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 1;
        [JsonPropertyName("overrides")] public Dictionary<string, List<string>> Overrides { get; set; } = new();
    }

    private static readonly Dictionary<string, IReadOnlySet<Job>> ByClassName = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>Where overrides are persisted. Set once at startup by the game layer.</summary>
    public static string? FilePath { get; set; }

    public static int Count => ByClassName.Count;

    public static IReadOnlyDictionary<string, IReadOnlySet<Job>> All => ByClassName;

    /// <summary>An override of an empty set is meaningful: it means "this card does no job".</summary>
    public static bool TryGet(string cardClassName, out IReadOnlySet<Job> jobs) =>
        ByClassName.TryGetValue(cardClassName, out jobs!);

    public static bool Has(string cardClassName) => ByClassName.ContainsKey(cardClassName);

    public static void Load(string? path = null)
    {
        path ??= FilePath;
        ByClassName.Clear();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            var file = JsonSerializer.Deserialize<OverrideFile>(File.ReadAllText(path));
            foreach (var pair in file?.Overrides ?? new Dictionary<string, List<string>>())
            {
                var jobs = new HashSet<Job>();
                foreach (var name in pair.Value ?? new List<string>())
                {
                    if (Enum.TryParse<Job>(name, ignoreCase: true, out var job))
                    {
                        jobs.Add(job);
                    }
                    else
                    {
                        ModLog.Warn($"Ignoring unknown job '{name}' for {pair.Key} in the override file");
                    }
                }

                if (jobs.Contains(Job.FrontloadedAoe))
                {
                    jobs.Add(Job.FrontloadedDamage);
                }

                ByClassName[pair.Key] = jobs;
            }
        }
        catch (Exception ex)
        {
            ModLog.Error($"Could not read overrides from {path}, ignoring them: {ex.Message}");
            ByClassName.Clear();
        }
    }

    /// <summary>Records an override and persists it. Pass an empty set to mean "no jobs".</summary>
    public static void Set(string cardClassName, IEnumerable<Job> jobs, string? path = null)
    {
        var set = new HashSet<Job>(jobs);
        if (set.Contains(Job.FrontloadedAoe))
        {
            set.Add(Job.FrontloadedDamage);
        }
        ByClassName[cardClassName] = set;
        Save(path);
    }

    /// <summary>Removes an override, returning the card to whatever the shipped tables say.</summary>
    public static void Clear(string cardClassName, string? path = null)
    {
        if (ByClassName.Remove(cardClassName))
        {
            Save(path);
        }
    }

    public static void Save(string? path = null)
    {
        path ??= FilePath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            var file = new OverrideFile();
            foreach (var pair in ByClassName.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                file.Overrides[pair.Key] = pair.Value.OrderBy(j => j.ToString(), StringComparer.Ordinal).Select(j => j.ToString()).ToList();
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, JsonSerializer.Serialize(file, WriteOptions));
        }
        catch (Exception ex)
        {
            ModLog.Error($"Could not save overrides to {path}: {ex.Message}");
        }
    }

    /// <summary>Test seam: drops everything in memory without touching disk.</summary>
    public static void ResetForTests()
    {
        ByClassName.Clear();
        FilePath = null;
    }
}
