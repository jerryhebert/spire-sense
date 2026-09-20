namespace SpireSense.SpireSenseCode.Jobs;

/// <summary>
/// Sizing arithmetic for the on-screen panels. Lives here, free of engine types, so the clamping
/// and stepping are unit-testable; the settings class just stores the value and calls in.
/// </summary>
public static class PanelScale
{
    /// <summary>Half size. Smaller than this the text stops being readable.</summary>
    public const float Min = 0.5f;

    /// <summary>Double size, i.e. 100% larger than designed.</summary>
    public const float Max = 2.0f;

    public const float Default = 1.0f;

    /// <summary>Brings any value into range, including one from a hand-edited settings file.</summary>
    public static float Clamp(float value)
    {
        if (!float.IsFinite(value))
        {
            return Default;
        }
        return Math.Clamp(value, Min, Max);
    }

    /// <summary>
    /// The scale implied by dragging a corner grip to a point <paramref name="offsetX"/>,
    /// <paramref name="offsetY"/> from the panel's top-left, where the panel's unscaled size is
    /// <paramref name="baseWidth"/> by <paramref name="baseHeight"/>.
    ///
    /// The larger of the two ratios wins, so dragging along either axis alone still resizes at full
    /// rate rather than half. Returns <paramref name="fallback"/> if the panel has not been laid out
    /// yet and has no size to measure against.
    /// </summary>
    public static float FromDrag(float offsetX, float offsetY, float baseWidth, float baseHeight, float fallback)
    {
        if (baseWidth <= 0f || baseHeight <= 0f)
        {
            return Clamp(fallback);
        }

        return Clamp(Math.Max(offsetX / baseWidth, offsetY / baseHeight));
    }

    /// <summary>Renders a scale as a whole-number percentage, e.g. "120%".</summary>
    public static string Label(float value) => $"{Math.Round(Clamp(value) * 100)}%";
}
