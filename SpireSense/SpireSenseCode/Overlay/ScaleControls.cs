using Godot;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// The shared minus / percentage / plus row. Both panels use one so they stay consistent and
/// read their size from the same setting.
/// </summary>
internal static class ScaleControls
{
    /// <summary>
    /// Builds the row. <paramref name="onChanged"/> runs after each step so the caller can reapply
    /// the scale to its own panel.
    /// </summary>
    public static HBoxContainer Build(OverlaySettings settings, int fontSize, Action onChanged)
    {
        var row = new HBoxContainer { Name = "ScaleRow", MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 4);

        var readout = new Label
        {
            Text = settings.ScaleLabel,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(52, 0),
        };
        readout.AddThemeFontSizeOverride("font_size", fontSize);
        readout.AddThemeColorOverride("font_color", new Color("9a9a9a"));

        Button Step(string text, int steps)
        {
            var button = new Button
            {
                Text = text,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(30, 0),
            };
            button.AddThemeFontSizeOverride("font_size", fontSize);
            button.Pressed += () =>
            {
                settings.AdjustScale(steps);
                readout.Text = settings.ScaleLabel;
                onChanged();
            };
            return button;
        }

        var caption = new Label { Text = "Size", MouseFilter = Control.MouseFilterEnum.Ignore };
        caption.AddThemeFontSizeOverride("font_size", fontSize);
        caption.AddThemeColorOverride("font_color", new Color("9a9a9a"));

        row.AddChild(caption);
        row.AddChild(Step("-", -1));
        row.AddChild(readout);
        row.AddChild(Step("+", 1));
        return row;
    }
}
