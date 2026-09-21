using System.Reflection;
using SpireSense.SpireSenseCode.Categories;

namespace SpireSense.Tests;

internal static class TestData
{
    /// <summary>The test assembly embeds the real category tables under the same resource names as the mod.</summary>
    public static Assembly TablesAssembly => typeof(TestData).Assembly;

    public static void LoadRealTables() => CategoryDatabase.Load(TablesAssembly);

    /// <summary>A card name guaranteed not to appear in the curated tables, to exercise the heuristic.</summary>
    public const string UnknownCardName = "ZzzDefinitelyNotARealCard";

    public static CardFacts Attack(string name, decimal damage, bool allEnemies = false) =>
        CardFacts.Named(name, CardKind.Attack) with { Damage = damage, TargetsAllEnemies = allEnemies };

    public static CardFacts Skill(string name, decimal block = 0, decimal draw = 0) =>
        CardFacts.Named(name, CardKind.Skill) with { Block = block, Draw = draw, TargetsSelf = true };

    public static CardFacts Power(string name) => CardFacts.Named(name, CardKind.Power);

    public static CardFacts Curse(string name) => CardFacts.Named(name, CardKind.Curse);

    public static CardFacts Status(string name) => CardFacts.Named(name, CardKind.Status);
}
