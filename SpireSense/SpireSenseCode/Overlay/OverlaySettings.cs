using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
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
    [JsonPropertyName("toggle_key")] public string ToggleKey { get; set; } = "F8";
    [JsonPropertyName("show_card_names")] public bool ShowCardNames { get; set; } = true;

    public Key ParsedToggleKey => Enum.TryParse<Key>(ToggleKey, ignoreCase: true, out var key) ? key : Key.F8;

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
            SpireSenseMod.Logger.Warn($"Could not read overlay settings, using defaults: {ex.Message}");
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
            SpireSenseMod.Logger.Warn($"Could not save overlay settings: {ex.Message}");
        }
    }
}
