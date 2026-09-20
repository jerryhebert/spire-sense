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

    public const float Step = 0.1f;
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
    /// Moves <paramref name="steps"/> increments from <paramref name="current"/>. Snaps to the step
    /// grid so repeated clicks cannot accumulate floating point drift into values like 79.999%.
    /// </summary>
    public static float Adjust(float current, int steps)
    {
        var raw = Clamp(current) + steps * Step;
        var snapped = (float)Math.Round(raw / Step) * Step;
        return Clamp(snapped);
    }

    /// <summary>Renders a scale as a whole-number percentage, e.g. "120%".</summary>
    public static string Label(float value) => $"{Math.Round(Clamp(value) * 100)}%";
}
