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
        ApplyPatches(_harmony, Assembly.GetExecutingAssembly());
        ReportPatches(_harmony);

        SpireSenseOverlay.Install();
        if (!CardJobTip.IsAvailable)
        {
            ModLog.Warn("Card hover tips are unavailable: the HoverTip layout changed in this game build.");
        }
        ModLog.Info($"Spire Sense ready. Press {OverlaySettings.Current.ToggleKey} to toggle the overlay.");
    }

    /// <summary>
    /// Applies each patch class separately rather than through PatchAll.
    ///
    /// PatchAll aborts on the first class that throws, so one bad patch took the whole mod down
    /// with it: no overlay, no tooltips, nothing, for a fault in one feature. Patching class by
    /// class means a broken patch disables only its own feature and says so in the log.
    /// </summary>
    private static void ApplyPatches(Harmony harmony, Assembly assembly)
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
        {
            if (!type.HasHarmonyAttribute())
            {
                continue;
            }

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                ModLog.Error($"Patch class {type.Name} failed to apply, so its feature is off: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Names every method actually patched, and complains about any expected one that is missing.
    /// Harmony skips a patch class silently when it has no type-level attribute, which cost a
    /// release once: the feature simply did nothing and nothing in the log said so.
    /// </summary>
    private static void ReportPatches(Harmony harmony)
    {
        var patched = harmony.GetPatchedMethods()
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        ModLog.Info($"Harmony patches applied ({patched.Count}): {string.Join(", ", patched)}");

        var expected = new[]
        {
            "CardModel.get_HoverTips",
            "NInspectCardScreen._Ready",
            "NInspectCardScreen.UpdateCardDisplay",
            "CreatureCmd.Damage",
            "CreatureCmd.GainBlock",
            "CardPileCmd.DrawInternal",
        };

        var missing = expected.Where(e => !patched.Contains(e)).ToList();
        if (missing.Count > 0)
        {
            ModLog.Error($"These patches did not apply, so the features behind them are dead: {string.Join(", ", missing)}");
        }
    }
}
