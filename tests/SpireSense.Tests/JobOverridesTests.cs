using SpireSense.SpireSenseCode.Jobs;
using Xunit;

namespace SpireSense.Tests;

public class JobOverridesTests : IDisposable
{
    private readonly string _path;

    public JobOverridesTests()
    {
        TestData.LoadRealTables();
        JobOverrides.ResetForTests();
        _path = Path.Combine(Path.GetTempPath(), $"spiresense-overrides-{Guid.NewGuid():N}.json");
        JobOverrides.FilePath = _path;
    }

    public void Dispose()
    {
        JobOverrides.ResetForTests();
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void AnOverrideSurvivesAReload()
    {
        JobOverrides.Set("BodySlam", new[] { Job.FrontloadedDamage, Job.CardDraw });

        JobOverrides.ResetForTests();
        JobOverrides.FilePath = _path;
        JobOverrides.Load();

        Assert.True(JobOverrides.TryGet("BodySlam", out var jobs));
        Assert.Equal(new[] { Job.CardDraw, Job.FrontloadedDamage }, jobs.OrderBy(j => j.ToString()));
    }

    [Fact]
    public void AnEmptyOverrideMeansNoJobsRatherThanNoOverride()
    {
        // Clearing every toggle has to be distinguishable from never having touched the card.
        JobOverrides.Set("StrikeIronclad", Array.Empty<Job>());

        Assert.True(JobOverrides.Has("StrikeIronclad"));
        Assert.True(JobOverrides.TryGet("StrikeIronclad", out var jobs));
        Assert.Empty(jobs);
    }

    [Fact]
    public void ClearingRestoresTheShippedClassification()
    {
        JobOverrides.Set("StrikeIronclad", new[] { Job.Scaling });
        Assert.Equal(ClassificationSource.Override, Classify("StrikeIronclad").Source);

        JobOverrides.Clear("StrikeIronclad");

        var after = Classify("StrikeIronclad");
        Assert.Equal(ClassificationSource.Curated, after.Source);
        Assert.Contains(Job.FrontloadedDamage, after.Jobs);
    }

    [Fact]
    public void MarkingACardAoeAlsoMarksItFrontloadedDamage()
    {
        JobOverrides.Set("StrikeIronclad", new[] { Job.FrontloadedAoe });

        Assert.True(JobOverrides.TryGet("StrikeIronclad", out var jobs));
        Assert.Contains(Job.FrontloadedDamage, jobs);
    }

    [Fact]
    public void UnknownJobNamesInTheFileAreIgnoredRatherThanFatal()
    {
        File.WriteAllText(_path, """
        { "version": 1, "overrides": { "Bash": ["Scaling", "NotARealJob"] } }
        """);

        JobOverrides.Load();

        Assert.True(JobOverrides.TryGet("Bash", out var jobs));
        Assert.Equal(new[] { Job.Scaling }, jobs);
    }

    [Fact]
    public void ACorruptFileIsIgnoredInsteadOfBreakingStartup()
    {
        File.WriteAllText(_path, "{ this is not json");

        JobOverrides.Load();

        Assert.Equal(0, JobOverrides.Count);
    }

    [Fact]
    public void AMissingFileIsNormalOnFirstRun()
    {
        JobOverrides.Load();

        Assert.Equal(0, JobOverrides.Count);
    }

    [Fact]
    public void OverridesBeatTheShippedTables()
    {
        var before = Classify("DemonForm");
        Assert.Equal(ClassificationSource.Curated, before.Source);

        JobOverrides.Set("DemonForm", new[] { Job.CardDraw });

        var after = Classify("DemonForm");
        Assert.Equal(ClassificationSource.Override, after.Source);
        Assert.Equal(new[] { Job.CardDraw }, after.Jobs);
    }

    [Fact]
    public void OverridesAlsoApplyToCardsTheTablesDoNotKnow()
    {
        JobOverrides.Set(TestData.UnknownCardName, new[] { Job.Scaling });

        var result = Classify(TestData.UnknownCardName);

        Assert.Equal(ClassificationSource.Override, result.Source);
        Assert.Equal(new[] { Job.Scaling }, result.Jobs);
    }

    [Fact]
    public void CursesStayIgnoredEvenIfOverridden()
    {
        JobOverrides.Set("Regret", new[] { Job.Scaling });

        var result = CardClassifier.Classify(TestData.Curse("Regret"));

        Assert.Equal(ClassificationSource.Ignored, result.Source);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public void OverriddenCardsCountInTheDeckTotals()
    {
        JobOverrides.Set("StrikeIronclad", new[] { Job.Scaling });

        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Equal(1, analysis.Counts[Job.Scaling]);
        Assert.Equal(0, analysis.Counts[Job.FrontloadedDamage]);
        // An override is a deliberate decision, not a guess, so it must not be reported as one.
        Assert.Empty(analysis.GuessedCardNames);
        Assert.Equal(0, analysis.GuessedCounts[Job.Scaling]);
    }

    private static Classification Classify(string className) =>
        CardClassifier.Classify(CardFacts.Named(className, CardKind.Skill));
}
