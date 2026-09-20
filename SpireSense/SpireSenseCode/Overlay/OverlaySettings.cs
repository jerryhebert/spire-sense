using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpireSense.SpireSenseCode.Jobs;
using FileAccess = Godot.FileAccess;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// Per-user overlay preferences, stored as user://spiresense.json (the game's own user-data folder).
/// </summary>
public sealed class OverlaySettings
{
    private const string Path = "user://spiresense.json";

    [JsonPropertyName("visible")] public bool Visible { get; set; } = true;
    [JsonPropertyName("x")] public float X { get; set; } = 16f;
    [JsonPropertyName("y")] public float Y { get; set; } = 120f;
    [JsonPropertyName("font_size")] public int FontSize { get; set; } = 20;
    /// <summary>
    /// Name of a Godot <see cref="Key"/>, e.g. "Insert", "F9", "Backslash". Rebindable in game.
    /// Insert is the default because it is rarely claimed by the game or by other mods; F8 was not.
    /// </summary>
    [JsonPropertyName("toggle_key")] public string ToggleKey { get; set; } = DefaultToggleKey;
    [JsonPropertyName("show_card_names")] public bool ShowCardNames { get; set; } = true;
    [JsonPropertyName("show_card_tips")] public bool ShowCardTips { get; set; } = true;

    /// <summary>
    /// Size of both panels, 1.0 being their designed size. Applied as a Godot scale so everything
    /// inside scales together rather than only the text.
    /// </summary>
    [JsonPropertyName("scale")] public float Scale { get; set; } = 1f;

    /// <summary>
    /// Where the reclassification panel sits on the inspect screen. Null until you drag it, which
    /// lets the default follow the window size instead of being baked in.
    /// </summary>
    [JsonPropertyName("editor_x")] public float? EditorX { get; set; }
    [JsonPropertyName("editor_y")] public float? EditorY { get; set; }

    /// <summary>The settings in effect. Loaded once during mod initialization.</summary>
    public static OverlaySettings Current { get; private set; } = new();

    public const string DefaultToggleKey = "Insert";

    /// <summary>The scale actually applied, guarding against a hand-edited settings file.</summary>
    [JsonIgnore]
    public float ClampedScale => PanelScale.Clamp(Scale);

    [JsonIgnore]
    public string ScaleLabel => PanelScale.Label(Scale);

    /// <summary>Steps the scale and saves. Returns the new value.</summary>
    public float AdjustScale(int steps)
    {
        Scale = PanelScale.Adjust(Scale, steps);
        Save();
        return Scale;
    }

    /// <summary>Computed from <see cref="ToggleKey"/>; JsonIgnore keeps it out of the saved file.</summary>
    [JsonIgnore]
    public Key ParsedToggleKey =>
        Enum.TryParse<Key>(ToggleKey, ignoreCase: true, out var key) ? key : Key.Insert;

    /// <summary>How the current hotkey should read on screen.</summary>
    [JsonIgnore]
    public string ToggleKeyLabel => ParsedToggleKey.ToString();

    /// <summary>Loads settings from disk and publishes them as <see cref="Current"/>.</summary>
    public static OverlaySettings LoadAsCurrent()
    {
        Current = Load();
        return Current;
    }

    public static OverlaySettings Load()
    {
        try
        {
            if (FileAccess.FileExists(Path))
            {
                using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
                var json = file.GetAsText();
                var loaded = JsonSerializer.Deserialize<OverlaySettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not read overlay settings, using defaults: {ex.Message}");
        }

        return new OverlaySettings();
    }

    public void Save()
    {
        try
        {
            using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
            file.StoreString(JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not save overlay settings: {ex.Message}");
        }
    }
}
