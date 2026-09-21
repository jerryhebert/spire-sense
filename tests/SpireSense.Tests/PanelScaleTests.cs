using SpireSense.SpireSenseCode.Categories;
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
    public void DraggingToThePanelsOwnCornerLeavesTheScaleUnchanged()
    {
        // Grabbing the grip must not make the panel jump: at the moment of grabbing, the cursor is
        // exactly at the scaled bottom-right, which has to read back as the current scale.
        const float baseWidth = 300f, baseHeight = 200f, current = 1.4f;

        var result = PanelScale.FromDrag(baseWidth * current, baseHeight * current, baseWidth, baseHeight, current);

        Assert.Equal(current, result, 3);
    }

    [Fact]
    public void DraggingAlongOneAxisStillResizesAtFullRate()
    {
        // Dragging right only: the horizontal ratio must win rather than being averaged down.
        var result = PanelScale.FromDrag(offsetX: 360f, offsetY: 200f, baseWidth: 300f, baseHeight: 200f, fallback: 1f);

        Assert.Equal(1.2f, result, 3);
    }

    [Fact]
    public void DraggingInwardShrinksAndStopsAtTheFloor()
    {
        var result = PanelScale.FromDrag(offsetX: 10f, offsetY: 10f, baseWidth: 300f, baseHeight: 200f, fallback: 1f);

        Assert.Equal(PanelScale.Min, result, 3);
    }

    [Fact]
    public void DraggingOutwardStopsAtTheCeiling()
    {
        var result = PanelScale.FromDrag(offsetX: 9000f, offsetY: 9000f, baseWidth: 300f, baseHeight: 200f, fallback: 1f);

        Assert.Equal(PanelScale.Max, result, 3);
    }

    [Theory]
    [InlineData(0f, 200f)]
    [InlineData(300f, 0f)]
    public void APanelWithNoSizeYetKeepsItsCurrentScale(float baseWidth, float baseHeight)
    {
        // Before first layout the panel measures zero, and dividing by it would give infinity.
        var result = PanelScale.FromDrag(100f, 100f, baseWidth, baseHeight, fallback: 1.3f);

        Assert.Equal(1.3f, result, 3);
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
