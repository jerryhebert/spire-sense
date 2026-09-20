using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using SpireSense.SpireSenseCode.Jobs;
using SpireSense.SpireSenseCode.Overlay;

namespace SpireSense.SpireSenseCode;

/// <summary>
/// Mod entry point. The game calls <see cref="Initialize"/> once after loading the assembly.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class SpireSenseMod
{
    public const string ModId = "SpireSense";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    private static Harmony? _harmony;

    public static void Initialize()
    {
        Logger.Info("Initializing Spire Sense");

        JobDatabase.Load();
        Logger.Info($"Loaded job classifications for {JobDatabase.CardCount} cards across {JobDatabase.PoolCount} pools");

        _harmony = new Harmony(ModId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        SpireSenseOverlay.Install();
        Logger.Info("Spire Sense ready. Press F8 to toggle the overlay.");
    }
}
