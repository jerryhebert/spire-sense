using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Game;

/// <summary>
/// Copies the handful of values the classifier needs out of the game's card model.
/// This is the only place that knows about the game's card types.
/// </summary>
public static class CardFactsReader
{
    public static CardFacts Read(CardModel card)
    {
        var className = card.GetType().Name;

        try
        {
            var vars = card.DynamicVars;
            return new CardFacts(
                ClassName: className,
                DisplayName: DisplayName(card, className),
                Kind: MapKind(card.Type),
                TargetsAllEnemies: card.TargetType == TargetType.AllEnemies,
                TargetsSelf: card.TargetType == TargetType.Self,
                Damage: Value(vars, "Damage"),
                Block: Value(vars, "Block"),
                GainsBlock: card.GainsBlock,
                Draw: Value(vars, "Cards"),
                StrengthGain: Value(vars, "StrengthPower"),
                DexterityGain: Value(vars, "DexterityPower"),
                EnergyCost: card.EnergyCost?.Canonical ?? 0,
                // An X-cost card eats whatever is left, so it cannot be costed up front.
                CostsX: card.EnergyCost?.CostsX ?? false);
        }
        catch (Exception ex)
        {
            // A card whose data cannot be read still counts toward the deck total and can still be
            // matched against the curated table by name; only the heuristic loses its inputs.
            ModLog.Warn($"Could not read card data for {className}: {ex.Message}");
            return CardFacts.Named(className, MapKind(card.Type));
        }
    }

    private static CardKind MapKind(CardType type) => type switch
    {
        CardType.Attack => CardKind.Attack,
        CardType.Skill => CardKind.Skill,
        CardType.Power => CardKind.Power,
        CardType.Status => CardKind.Status,
        CardType.Curse => CardKind.Curse,
        _ => CardKind.Other,
    };

    private static decimal Value(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVarSet vars, string key) =>
        vars.TryGetValue(key, out var v) ? v.BaseValue : 0m;

    private static string DisplayName(CardModel card, string fallback)
    {
        try
        {
            var title = card.TitleLocString.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }
        catch
        {
            // Localization can be unavailable very early; fall through to the class name.
        }

        return fallback;
    }
}
