# How cards are classified

The five jobs come from
[Solving the Spire with Jobs](https://sts2.untapped.gg/en/articles/slay-the-spire-deckbuilding-strategy-solving-the-spire-with-jobs).
This file is the source of truth for what each one means. The tables in this folder are produced by
reading every card's implementation against it.

## The rule that matters most

**Evaluate all five jobs independently for every card.** They are not a taxonomy to file a card
under. Ask five separate yes/no questions and tag every yes. Most cards do more than one job, and a
card's headline effect never rules the others out.

The first pass got this wrong and produced two kinds of error, both worth keeping in mind:

- **Inferno** was tagged `Scaling` only. Its own note read "6 dmg to all enemies whenever you lose
  HP". It is a power, so "power, therefore scaling" ended the analysis before anyone asked whether
  it deals area damage. It does. It is `Scaling` **and** `Aoe`.
- **Rage** was tagged `FrontloadedBlock` only. It gives 3 block per attack played this turn, so the
  block it produces grows with how much you play. It is `FrontloadedBlock` **and** `Scaling`.

In both cases the fact that proved the missing tag was already written in the card's own note. The
failure was not knowledge, it was stopping at the first answer.

## The jobs

### FrontloadedDamage

Deals meaningful damage to enemies the turn it is played, with no setup. Strikes, Bash, Bludgeon.

Not this: a power, whose damage arrives later; an attack whose damage is negligible without prior
setup; damage that needs a resource you must first accumulate.

### Aoe

**Damages every enemy, whenever that damage happens.** A one-shot sweep, a power that hits all
enemies each turn, a delayed bomb, a payoff that hits all enemies once a condition is met.

This is deliberately **not** a subset of `FrontloadedDamage`. The question a player asks is "does
this deck have an answer to three enemies at once", and a power that hits all enemies every turn
answers it. Tag `Aoe` on its own merits, and tag `FrontloadedDamage` too only if the card also meets
that bar.

Multi-hit random-target attacks are not `Aoe` unless they reliably spread across enemies.
Applying a debuff to all enemies is not `Aoe`; damage is.

### FrontloadedBlock

Prevents damage the turn it is played, with no setup. Block cards, and also weakening or stunning
every enemy, or becoming intangible, since those stop damage just as block does.

Not this: a power that accrues block over later turns, which is `Scaling`.

### Scaling

Makes the deck do more the longer it goes on, or the more you play. Three shapes, all count:

1. **Across the fight.** Powers, permanent Strength, Dexterity or Focus, orb or poison engines,
   anything that repeats every turn.
2. **Within a turn.** Effects that grow with what you play this turn, such as Rage's block per
   attack, or a cost that falls as you play more. These run out at end of turn and are still
   scaling: the deck does more the more it does.
3. **Payoffs and enablers.** Cards that are weak alone but multiply a strategy: exhaust payoffs,
   discard payoffs, shiv or minion synergies, cards that double or replay other cards.

Not this: a single large hit, however large. Applying Vulnerable or Weak by itself.

### CardDraw

Draws, scries, tutors, retains, puts cards on top of the draw pile, or permanently thins the deck by
exhausting cards out of it. Anything that improves what you hold.

Not this: exhausting a single card from hand for an effect, which is a cost rather than thinning.

## Format

`jobs.<pool>.json`, keyed by the card's C# class name in the game assembly:

```json
{ "pool": "ironclad", "cards": { "Inferno": { "jobs": ["Scaling", "Aoe"], "note": "power: 6 dmg to all enemies when you lose HP" } } }
```

`note` is one short line of evidence for the tags. If a note states a fact, the tags must reflect
it; that mismatch is exactly how the first pass went wrong, and `JobTableDataTests` now checks for
it mechanically.
