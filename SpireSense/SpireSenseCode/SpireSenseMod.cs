using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
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

        JobDatabase.Load(Assembly.GetExecutingAssembly());
        ModLog.Info($"Loaded job classifications for {JobDatabase.CardCount} cards across {JobDatabase.PoolCount} pools");
        if (JobDatabase.Duplicates.Count > 0)
        {
            ModLog.Warn($"Duplicate card entries across job tables: {string.Join(", ", JobDatabase.Duplicates)}");
        }

        _harmony = new Harmony(ModId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        SpireSenseOverlay.Install();
        ModLog.Info("Spire Sense ready. Press F8 to toggle the overlay.");
    }
}
