using System.Text.Json;
using SpireSense.SpireSenseCode.Jobs;
using Xunit;

namespace SpireSense.Tests;

/// <summary>
/// Guards the shipped classification data itself. The tables are large and hand-curated, so these
/// tests are what stop a bad edit from silently shipping.
/// </summary>
public class JobTableDataTests
{
    public JobTableDataTests() => TestData.LoadRealTables();

    [Fact]
    public void LoadsEveryPoolTable()
    {
        Assert.Equal(6, JobDatabase.PoolCount);
    }

    [Fact]
    public void CoversTheWholeCardPool()
    {
        // Ironclad 90 + Silent 91 + Defect 91 + Necrobinder 91 + Regent 91 + Colorless 65.
        Assert.Equal(519, JobDatabase.CardCount);
    }

    [Fact]
    public void NoCardAppearsInTwoTables()
    {
        // A duplicate would silently discard one table's verdict for that card.
        Assert.Empty(JobDatabase.Duplicates);
    }

    [Fact]
    public void EveryAoeCardAlsoCountsAsFrontloadedDamage()
    {
        var offenders = JobDatabase.All
            .Where(e => e.Value.Contains(Job.FrontloadedAoe) && !e.Value.Contains(Job.FrontloadedDamage))
            .Select(e => e.Key)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void EveryJobNameInTheRawFilesIsRecognized()
    {
        // JobDatabase skips unknown job names with a warning, so a typo would not fail loading.
        // Reading the raw JSON catches it instead.
        var valid = Enum.GetNames<Job>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();

        foreach (var (resource, root) in ReadRawTables())
        {
            foreach (var card in root.GetProperty("cards").EnumerateObject())
            {
                foreach (var job in card.Value.GetProperty("jobs").EnumerateArray())
                {
                    var name = job.GetString();
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
    public void MostCardsHaveAtLeastOneJob()
    {
        // Empty is legal for pure utility cards, but a large jump would mean the data regressed.
        var empty = JobDatabase.All.Count(e => e.Value.Count == 0);
        Assert.InRange(empty, 0, 60);
    }

    [Theory]
    // Spot checks across every pool, including multi-job cards.
    [InlineData("StrikeIronclad", Job.FrontloadedDamage)]
    [InlineData("DefendIronclad", Job.FrontloadedBlock)]
    [InlineData("DemonForm", Job.Scaling)]
    [InlineData("Thunderclap", Job.FrontloadedAoe)]
    [InlineData("BattleTrance", Job.CardDraw)]
    [InlineData("ShrugItOff", Job.FrontloadedBlock)]
    [InlineData("ShrugItOff", Job.CardDraw)]
    [InlineData("NoxiousFumes", Job.Scaling)]
    [InlineData("Defragment", Job.Scaling)]
    public void KnownCardsHaveTheExpectedJob(string cardClassName, Job expected)
    {
        Assert.True(JobDatabase.TryGet(cardClassName, out var jobs), $"{cardClassName} is missing from the tables");
        Assert.Contains(expected, jobs);
    }

    [Fact]
    public void ThunderclapCountsAsBothDamageAndAoe()
    {
        Assert.True(JobDatabase.TryGet("Thunderclap", out var jobs));
        Assert.Contains(Job.FrontloadedDamage, jobs);
        Assert.Contains(Job.FrontloadedAoe, jobs);
    }

    private static IEnumerable<(string Resource, JsonElement Root)> ReadRawTables()
    {
        var assembly = TestData.TablesAssembly;
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(JobDatabase.ResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);
            yield return (name, doc.RootElement.Clone());
        }
    }
}
