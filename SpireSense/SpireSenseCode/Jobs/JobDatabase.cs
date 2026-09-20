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

    private static readonly Dictionary<string, IReadOnlySet<Job>> _byClassName = new(StringComparer.Ordinal);

    public static int CardCount => _byClassName.Count;
    public static int PoolCount { get; private set; }

    public static void Load()
    {
        _byClassName.Clear();
        PoolCount = 0;

        var assembly = Assembly.GetExecutingAssembly();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith("SpireSense.Data.jobs.", StringComparison.Ordinal) || !resourceName.EndsWith(".json", StringComparison.Ordinal))
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
                    SpireSenseMod.Logger.Warn($"Job table {resourceName} has no cards");
                    continue;
                }

                PoolCount++;
                foreach (var (className, entry) in file.Cards)
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
                            SpireSenseMod.Logger.Warn($"Unknown job '{jobName}' on card {className} in {resourceName}");
                        }
                    }

                    // AoE is defined as a subset of frontloaded damage; keep the data consistent.
                    if (jobs.Contains(Job.FrontloadedAoe))
                    {
                        jobs.Add(Job.FrontloadedDamage);
                    }

                    if (_byClassName.ContainsKey(className))
                    {
                        SpireSenseMod.Logger.Warn($"Card {className} appears in more than one job table; keeping the first");
                        continue;
                    }

                    _byClassName[className] = jobs;
                }
            }
            catch (Exception ex)
            {
                SpireSenseMod.Logger.Error($"Failed to load job table {resourceName}: {ex}");
            }
        }
    }

    public static bool TryGet(string cardClassName, out IReadOnlySet<Job> jobs)
    {
        return _byClassName.TryGetValue(cardClassName, out jobs!);
    }
}
