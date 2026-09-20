using SpireSense.SpireSenseCode.Jobs;
using Xunit;

namespace SpireSense.Tests;

public class PanelScaleTests
{
    [Fact]
    public void TheRangeIsHalfSizeToDoubleSize()
    {
        Assert.Equal(0.5f, PanelScale.Min);
        Assert.Equal(2.0f, PanelScale.Max);
    }

    [Theory]
    [InlineData(5f, 2.0f)]
    [InlineData(0.01f, 0.5f)]
    [InlineData(1.3f, 1.3f)]
    public void ValuesOutsideTheRangeAreBroughtBackIn(float input, float expected)
    {
        Assert.Equal(expected, PanelScale.Clamp(input), 3);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteValuesFallBackToTheDefault(float input)
    {
        // A hand-edited settings file can contain anything, and a NaN scale would make the panel
        // vanish rather than error.
        Assert.Equal(PanelScale.Default, PanelScale.Clamp(input), 3);
    }

    [Fact]
    public void SteppingUpAndDownMovesOneIncrement()
    {
        Assert.Equal(1.1f, PanelScale.Adjust(1.0f, 1), 3);
        Assert.Equal(0.9f, PanelScale.Adjust(1.0f, -1), 3);
    }

    [Fact]
    public void SteppingStopsAtTheLimits()
    {
        Assert.Equal(PanelScale.Max, PanelScale.Adjust(PanelScale.Max, 1), 3);
        Assert.Equal(PanelScale.Min, PanelScale.Adjust(PanelScale.Min, -1), 3);
    }

    [Fact]
    public void RepeatedSteppingDoesNotAccumulateDrift()
    {
        // Ten steps up then ten down must land exactly back on 100%, not on 99.9999%, or the
        // readout starts showing nonsense percentages.
        var value = 1.0f;
        for (var i = 0; i < 10; i++) value = PanelScale.Adjust(value, 1);
        for (var i = 0; i < 10; i++) value = PanelScale.Adjust(value, -1);

        Assert.Equal(1.0f, value, 4);
        Assert.Equal("100%", PanelScale.Label(value));
    }

    [Fact]
    public void SteppingFromTheTopAllTheWayDownReachesTheFloor()
    {
        var value = PanelScale.Max;
        for (var i = 0; i < 100; i++) value = PanelScale.Adjust(value, -1);

        Assert.Equal(PanelScale.Min, value, 3);
    }

    [Theory]
    [InlineData(1.0f, "100%")]
    [InlineData(0.5f, "50%")]
    [InlineData(2.0f, "200%")]
    [InlineData(1.15f, "115%")]
    public void LabelsReadAsWholePercentages(float input, string expected)
    {
        Assert.Equal(expected, PanelScale.Label(input));
    }
}
