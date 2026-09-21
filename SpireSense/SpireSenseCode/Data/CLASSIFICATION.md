# How cards are classified

Every card is scored against six **categories**. The tables in this folder are produced by reading
each card's implementation against the definitions below, and this file is the source of truth.

## The rule that matters most

**Evaluate all six categories independently for every card.** They are not a taxonomy to file a
card under. Ask six separate yes/no questions and tag every yes. Most cards do more than one thing,
and a card's headline effect never rules the others out.

An earlier pass got this wrong twice over, and both mistakes are worth keeping in mind:

- **Inferno** was tagged as scaling only. Its own note read "6 dmg to all enemies whenever you lose
  HP". Being a power ended the analysis before anyone asked whether it hits everything. It does.
- **Rage** was tagged as block only. It gives block per attack played this turn, so the block it
  produces grows with how much you play. It is block **and** scaling.

In both cases the fact that proved the missing tag was already written in the card's own note. The
failure was not knowledge, it was stopping at the first answer.

## The categories

### FrontloadedDamage

Deals meaningful damage to enemies **the turn it is played**, with no setup. Strikes, Bash,
Bludgeon. The test is whether it helps on turn one of a fight you walked into cold.

Not this: a power, whose damage arrives later. An attack whose damage is negligible without prior
setup. Damage gated behind a resource you must first accumulate.

### ScalingDamage

Damage that **grows or repeats** rather than landing once. Poison, a power that damages each turn,
permanent Strength, orb engines, a card that gets bigger every time you play it, exhaust or discard
payoffs that multiply your damage, effects that double or replay attacks.

This is the category for "the longer this fight goes, the more damage I do". A single enormous hit
is not scaling damage however large it is.

### Aoe

**Damages every enemy, whenever that damage lands.** A one-shot sweep, a power that hits all enemies
each turn, a delayed bomb, a payoff that hits everything once a condition is met.

Independent of the two damage categories above: tag those on their own merits and tag `Aoe` as well
when the damage reaches everything. Multi-hit random-target attacks are not `Aoe` unless they
reliably spread. A debuff applied to all enemies is not `Aoe`; damage is.

### FrontloadedBlock

Prevents damage **the turn it is played**, with no setup. Block cards, and also weakening or
stunning every enemy, or becoming intangible, since those stop damage just as block does.

### ScalingBlock

Defence that **grows or repeats**: a power granting block every turn, Plating, permanent Dexterity,
Barricade and block-retention effects, block that scales with what you play during the turn, block
payoffs that multiply other defence.

The Rage case lives here: block per attack played is scaling block as well as frontloaded block.

### Acceleration

Anything that makes the deck **go faster or do more per turn**: drawing, scrying, tutoring,
retaining, putting cards on top of the draw pile, permanently thinning the deck, gaining energy,
reducing costs, and effects that grant extra card plays.

This is the category for tempo and consistency. It replaces what was previously called card draw,
and is deliberately wider: energy and extra plays accelerate a deck exactly as drawing does.

Not this: exhausting a single card from hand as the cost of an effect.

## Format

`categories.<pool>.json`, keyed by the card's C# class name in the game assembly:

```json
{
  "pool": "ironclad",
  "cards": {
    "Inferno": { "categories": ["ScalingDamage", "Aoe"], "note": "power: 6 dmg to all enemies when you lose HP" }
  }
}
```

`note` is one short line of evidence for the tags. If a note states a fact, the tags must reflect
it; that mismatch is exactly how the first pass went wrong, and the tests now check for it
mechanically.
