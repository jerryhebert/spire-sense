using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using MegaCrit.Sts2.Core.Modding;
using SpireSense.SpireSenseCode.Game;
using SpireSense.SpireSenseCode.Jobs;
using SpireSense.SpireSenseCode.Overlay;

namespace SpireSense.SpireSenseCode;

/// <summary>Routes the mod's logging seam to the game's logger.</summary>
internal sealed class GameLog : ISpireSenseLog
{
    private readonly Logger _logger = new(SpireSenseMod.ModId, LogType.Generic);

    public void Info(string message) => _logger.Info(message);
    public void Warn(string message) => _logger.Warn(message);
    public void Error(string message) => _logger.Error(message);
}

/// <summary>
/// Mod entry point. The game calls <see cref="Initialize"/> once after loading the assembly.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class SpireSenseMod
{
    public const string ModId = "SpireSense";

    private static Harmony? _harmony;

    public static void Initialize()
    {
        ModLog.Current = new GameLog();
        ModLog.Info("Initializing Spire Sense");

        OverlaySettings.LoadAsCurrent();

        JobDatabase.Load(Assembly.GetExecutingAssembly());
        ModLog.Info($"Loaded job classifications for {JobDatabase.CardCount} cards across {JobDatabase.PoolCount} pools");

        // Your own reclassifications live next to the game's other user data so they survive
        // reinstalling or updating the mod.
        JobOverrides.FilePath = ProjectSettings.GlobalizePath("user://spiresense_overrides.json");
        JobOverrides.Load();
        if (JobOverrides.Count > 0)
        {
            ModLog.Info($"Applied {JobOverrides.Count} of your own card classifications");
        }
        if (JobDatabase.Duplicates.Count > 0)
        {
            ModLog.Warn($"Duplicate card entries across job tables: {string.Join(", ", JobDatabase.Duplicates)}");
        }

        // Registers this assembly's Node subclasses with Godot so the engine knows to call their
        // _Ready/_Process/_UnhandledKeyInput overrides. Without it the overlay node can be created
        // but never ticked.
        try
        {
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            ModLog.Warn($"Could not register mod scripts with Godot: {ex.Message}");
        }

        _harmony = new Harmony(ModId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        SpireSenseOverlay.Install();
        if (!CardJobTip.IsAvailable)
        {
            ModLog.Warn("Card hover tips are unavailable: the HoverTip layout changed in this game build.");
        }
        ModLog.Info("Spire Sense ready. Press F8 to toggle the overlay.");
    }
}
