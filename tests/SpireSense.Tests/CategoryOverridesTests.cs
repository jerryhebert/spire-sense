using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

public class CategoryOverridesTests : IDisposable
{
    private readonly string _path;

    public CategoryOverridesTests()
    {
        TestData.LoadRealTables();
        CategoryOverrides.ResetForTests();
        _path = Path.Combine(Path.GetTempPath(), $"spiresense-overrides-{Guid.NewGuid():N}.json");
        CategoryOverrides.FilePath = _path;
    }

    public void Dispose()
    {
        CategoryOverrides.ResetForTests();
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void AnOverrideSurvivesAReload()
    {
        CategoryOverrides.Set("BodySlam", new[] { Category.FrontloadedDamage, Category.Acceleration });

        CategoryOverrides.ResetForTests();
        CategoryOverrides.FilePath = _path;
        CategoryOverrides.Load();

        Assert.True(CategoryOverrides.TryGet("BodySlam", out var categories));
        Assert.Equal(new[] { Category.Acceleration, Category.FrontloadedDamage }, categories.OrderBy(j => j.ToString()));
    }

    [Fact]
    public void AnEmptyOverrideMeansNoCategoriesRatherThanNoOverride()
    {
        // Clearing every toggle has to be distinguishable from never having touched the card.
        CategoryOverrides.Set("StrikeIronclad", Array.Empty<Category>());

        Assert.True(CategoryOverrides.Has("StrikeIronclad"));
        Assert.True(CategoryOverrides.TryGet("StrikeIronclad", out var categories));
        Assert.Empty(categories);
    }

    [Fact]
    public void ClearingRestoresTheShippedClassification()
    {
        CategoryOverrides.Set("StrikeIronclad", new[] { Category.ScalingDamage });
        Assert.Equal(ClassificationSource.Override, Classify("StrikeIronclad").Source);

        CategoryOverrides.Clear("StrikeIronclad");

        var after = Classify("StrikeIronclad");
        Assert.Equal(ClassificationSource.Curated, after.Source);
        Assert.Contains(Category.FrontloadedDamage, after.Categories);
    }

    [Fact]
    public void AreaDamageCanBeSetOnItsOwn()
    {
        // It used to drag frontloaded damage along with it. A power that hits every enemy over
        // time is area damage and nothing else, so the two must be independently settable.
        CategoryOverrides.Set("StrikeIronclad", new[] { Category.Aoe });

        Assert.True(CategoryOverrides.TryGet("StrikeIronclad", out var categories));
        Assert.Equal(new[] { Category.Aoe }, categories);
    }

    [Fact]
    public void OverrideFilesWrittenBeforeTheRenameStillLoad()
    {
        // "FrontloadedAoe" was the old name for this category; an existing user file must not be lost.
        File.WriteAllText(_path, @"{ ""version"": 1, ""overrides"": { ""Bash"": [""FrontloadedAoe""] } }");

        CategoryOverrides.Load();

        Assert.True(CategoryOverrides.TryGet("Bash", out var categories));
        Assert.Equal(new[] { Category.Aoe }, categories);
    }

    [Fact]
    public void UnknownCategoryNamesInTheFileAreIgnoredRatherThanFatal()
    {
        File.WriteAllText(_path, """
        { "version": 1, "overrides": { "Bash": ["Scaling", "NotARealCategory"] } }
        """);

        CategoryOverrides.Load();

        Assert.True(CategoryOverrides.TryGet("Bash", out var categories));
        Assert.Equal(new[] { Category.ScalingDamage }, categories);
    }

    [Fact]
    public void ACorruptFileIsIgnoredInsteadOfBreakingStartup()
    {
        File.WriteAllText(_path, "{ this is not json");

        CategoryOverrides.Load();

        Assert.Equal(0, CategoryOverrides.Count);
    }

    [Fact]
    public void AMissingFileIsNormalOnFirstRun()
    {
        CategoryOverrides.Load();

        Assert.Equal(0, CategoryOverrides.Count);
    }

    [Fact]
    public void OverridesBeatTheShippedTables()
    {
        var before = Classify("DemonForm");
        Assert.Equal(ClassificationSource.Curated, before.Source);

        CategoryOverrides.Set("DemonForm", new[] { Category.Acceleration });

        var after = Classify("DemonForm");
        Assert.Equal(ClassificationSource.Override, after.Source);
        Assert.Equal(new[] { Category.Acceleration }, after.Categories);
    }

    [Fact]
    public void OverridesAlsoApplyToCardsTheTablesDoNotKnow()
    {
        CategoryOverrides.Set(TestData.UnknownCardName, new[] { Category.ScalingDamage });

        var result = Classify(TestData.UnknownCardName);

        Assert.Equal(ClassificationSource.Override, result.Source);
        Assert.Equal(new[] { Category.ScalingDamage }, result.Categories);
    }

    [Fact]
    public void CursesStayIgnoredEvenIfOverridden()
    {
        CategoryOverrides.Set("Regret", new[] { Category.ScalingDamage });

        var result = CardClassifier.Classify(TestData.Curse("Regret"));

        Assert.Equal(ClassificationSource.Ignored, result.Source);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public void OverriddenCardsCountInTheDeckTotals()
    {
        CategoryOverrides.Set("StrikeIronclad", new[] { Category.ScalingDamage });

        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });

        Assert.Equal(1, analysis.Counts[Category.ScalingDamage]);
        Assert.Equal(0, analysis.Counts[Category.FrontloadedDamage]);
        // An override is a deliberate decision, not a guess, so it must not be reported as one.
        Assert.Empty(analysis.GuessedCardNames);
        Assert.Equal(0, analysis.GuessedCounts[Category.ScalingDamage]);
    }

    private static Classification Classify(string className) =>
        CardClassifier.Classify(CardFacts.Named(className, CardKind.Skill));
}
