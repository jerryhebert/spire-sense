using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

public class CategoryTests
{
    [Fact]
    public void EveryCategoryHasBothAShortAndASpelledOutName()
    {
        Assert.Equal(6, CategoryInfo.All.Length);

        foreach (var category in CategoryInfo.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(CategoryInfo.DisplayName(category)));
            Assert.False(string.IsNullOrWhiteSpace(CategoryInfo.LongName(category)));
        }
    }

    [Theory]
    [InlineData(Category.FrontloadedDamage, "FL. Damage")]
    [InlineData(Category.ScalingDamage, "Sc. Damage")]
    [InlineData(Category.Aoe, "AOE")]
    [InlineData(Category.FrontloadedBlock, "FL. Block")]
    [InlineData(Category.ScalingBlock, "Sc. Block")]
    [InlineData(Category.Acceleration, "Acceleration")]
    public void ThePanelLabelsAreTheAbbreviatedOnes(Category category, string expected)
    {
        // The panel is narrow and these sit beside numbers that have to line up, so the short forms
        // are the product decision rather than an implementation detail. Pinned so a rename shows up.
        Assert.Equal(expected, CategoryInfo.DisplayName(category));
    }

    [Theory]
    [InlineData("FrontloadedDamage", Category.FrontloadedDamage)]
    [InlineData("frontloadeddamage", Category.FrontloadedDamage)]
    [InlineData("Acceleration", Category.Acceleration)]
    public void CurrentNamesParse(string name, Category expected)
    {
        Assert.True(CategoryInfo.TryParse(name, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    // Names this mod shipped before damage and block were split apart. An overrides file written
    // against them is still on someone's disk, and silently dropping their choices would be worse
    // than mapping each retired name to its closest survivor.
    [InlineData("Scaling", Category.ScalingDamage)]
    [InlineData("CardDraw", Category.Acceleration)]
    [InlineData("FrontloadedAoe", Category.Aoe)]
    public void RetiredNamesStillParseToTheirClosestSurvivor(string name, Category expected)
    {
        Assert.True(CategoryInfo.TryParse(name, out var parsed), $"{name} no longer parses");
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void NonsenseDoesNotParse()
    {
        Assert.False(CategoryInfo.TryParse("NotARealCategory", out _));
    }
}
