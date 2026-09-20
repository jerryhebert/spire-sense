# Spire Sense

An in-run overlay for Slay the Spire 2 that counts how many cards in your deck do each deckbuilding
"job", following the framework in
[Solving the Spire with Jobs](https://sts2.untapped.gg/en/articles/slay-the-spire-deckbuilding-strategy-solving-the-spire-with-jobs):

| Job | Meaning |
|---|---|
| Frontloaded Damage | Deals real damage the turn it is played, no setup needed |
| Area Damage | Damages every enemy, whenever that damage lands |
| Frontloaded Block | Prevents damage the turn it is played (block, weaken-all, intangible...) |
| Scaling | Makes the deck stronger as the fight goes on (powers, strength, engines) |
| Card Draw / Manipulation | Draw, scry, tutor, retain, top-decking, permanent thinning |

A card can count toward several jobs. Upgrades do not change a card's jobs. Curses and statuses are
counted separately and never contribute to a job.

## Deck power

The headline number at the top of the panel: one score out of 100 for how well the deck is holding
up, and the job dragging it down. `Jobs/DeckPower.cs`.

It is **not** a weighted sum, deliberately. A sum lets enormous damage paper over having no block,
which is how runs actually end; the framework's claim is that you lose to the job you are *missing*.
So each job gets an adequacy ratio, what it delivers over what this point in the run demands, and
those combine with a harmonic mean, which is dominated by the smallest of them. Meeting every demand
exactly scores 100. Surplus is capped, because twice the damage you need does not make up for half
the block you need.

Two of the four demands are grounded in real numbers rather than guesses:

- **Damage** is measured against the average starting health of an elite in your current act, read
  from the game's own encounter tables, over five turns. It rises by itself as you climb.
- **Block** is measured against the damage enemies are actually landing on you, which the mod
  watches the same way it watches your output. No table could be as accurate.
- **Scaling** and **card draw** are compared against per-act thresholds that are judgement, not
  measurement. They are the weakest inputs and are deliberately forgiving.

The limiting job is the actionable half. "62, held back by Block" tells you what to draft; "62" on
its own does not. Until there is something to compare against the panel says `measuring…` rather
than showing a confident zero.

**Unvalidated.** The weighting is reasoned, not fitted to outcomes. Treat it as a prompt to look at
the row it names, not as a verdict.

## Using it

- The panel shows nearly everywhere during a run: combat, the map, shops, events, rewards and the
  deck view. It hides only on the menus that are not about the run in front of you, namely settings,
  the compendium and the pause menu.
- It asks the game which screen is topmost rather than keeping its own list, and the list it does
  keep is of exclusions, so a screen added by a future game update shows the panel rather than
  silently losing it.
- **Insert** hides and shows it. Click and drag the panel to move it. Both are remembered.
- To rebind, click the hotkey button on the panel and press any key. Escape cancels rather than
  binding, so Escape cannot be used as the hotkey. Insert is the default because the game and other
  mods rarely claim it.
- Both panels resize from the grip in their bottom-right corner, between 50% and 200%. The whole
  panel scales, not just the text, and the size is shared so the two always match. The grip's
  tooltip shows the current percentage while you drag.
- The reclassification panel is draggable by its heading or background, since a card's own tooltips
  can sit over where it opens. Its position is remembered separately from the overlay's.
- Settings live in `%APPDATA%\SlayTheSpire2\spiresense.json` (created on first toggle, drag, resize
  or rebind): `visible`, `x`, `y`, `font_size`, `scale`, `editor_x`, `editor_y`, `toggle_key` (any
  Godot `Key` name), `show_card_names`, `show_card_tips`. Delete the file to reset everything, which
  is the way out if you ever hide the panel and bind the hotkey to something another mod has taken.
- Below a divider, a **Per turn** section: damage dealt and block gained in an average turn.
  - Measured from the run so far, and refined with every turn you play, so it gets steadier as the
    run goes on. Reset when a new run starts.
  - Per turn rather than per cycle because a cycle total is a moving target: the cycle lengthens
    every time the deck grows, so the same number means something different in Act 3 than it did in
    Act 1. A per-turn rate stays comparable across the whole run, and dead cards lower it, which is
    what you want to see.
  - Only your own output counts. In multiplayer a teammate's damage and block are theirs. Poison is
    the exception, since it passes no dealer at all: it counts in single-player and is skipped in
    co-op rather than credited to everyone.
  - Once you have played a turn these are **measured from the run so far**: every point of damage
    dealt to enemies and every point of block gained, averaged over the turns played. They get
    steadier as the run goes on, and reset when a new run starts.
  - Before that, and marked `estimated`, they are predicted from the deck by `Jobs/CycleEstimate.cs`:
    a cycle lasts as long as it takes to draw the deck, energy caps how much you get to play, and
    unplayable cards lengthen the cycle, so a curse lowers both numbers.
  - The measurement is the better figure. It sees Strength, relics, powers, orbs and multi-hit
    attacks, none of which the deck data reveals. Overkill is excluded, so hitting a 5 HP enemy for
    30 counts as 5.
- Each job shows the share of your deck doing it and the card count, e.g. "14% (9)". The percentage
  is of every card in the deck, curses and statuses included, since those are cards you still draw.
- Cards the mod has no table entry for are classified by a rough heuristic and shown as
  "(N guessed)" plus a "Guessed:" list. Cards where even the heuristic finds nothing appear under "No job:".

## Code layout

| Folder | Depends on the game? | Contents |
|---|---|---|
| `SpireSenseCode/Jobs/` | no | Job definitions, the curated table loader, the classifier, deck counting |
| `SpireSenseCode/Overlay/OverlayText.cs` | no | Panel text formatting |
| `SpireSenseCode/Overlay/` (rest) | yes | The Godot node, settings persistence |
| `SpireSenseCode/Game/` | yes | Reading the current run, copying card data out of the game's models, and the hover-tip and inspect-screen patches |

The split is deliberate. `CardFacts` is a plain snapshot of the handful of values a card contributes,
so the classification logic never touches a game type and can be unit-tested. `Game/CardFactsReader.cs`
is the only file that knows how to read the game's card model.

## How classification works

Three sources, in priority order:

1. **Your own overrides**, from the inspect-screen panel, stored in
   `%APPDATA%\SlayTheSpire2\spiresense_overrides.json`. An override with an empty job list is
   meaningful: it means "this card does no job". Deleting the file reverts everything.
2. **The curated tables** below.
3. **A heuristic**, for cards no table knows about.

- [`SpireSenseCode/Data/CLASSIFICATION.md`](SpireSenseCode/Data/CLASSIFICATION.md) defines what each
  job means and is the specification the tables are built from. Read it before changing a verdict.
- `SpireSenseCode/Data/jobs.<pool>.json` holds one curated entry per card, keyed by the card's C# class
  name in the game assembly (e.g. `PommelStrike`). Each entry has a `jobs` array and a `note`.
  These files are embedded into the dll at build time, so edit and rebuild to change a verdict.
- `Jobs/CardClassifier.cs` applies the table first and falls back to a heuristic (Power => Scaling,
  Attack with base damage => Frontloaded Damage, AllEnemies target => AoE, block => Frontloaded Block,
  Cards var => Card Draw, self Strength/Dexterity => Scaling).
- The tables were produced by reading each card's decompiled implementation against the job
  definitions above. Judgment calls are recorded in each card's `note`.

## Building and testing

From the repository root, `pwsh ./scripts/Run-Checks.ps1` runs the tests, the dependency audit and a
Release build. Tests alone: `dotnet test tests/SpireSense.Tests/SpireSense.Tests.csproj`.

Open `SpireSense.sln` in Rider and Build, or run `dotnet build -c Release` in this folder.
The build copies `SpireSense.dll`, `.pdb` and `.json` into the game's `mods\SpireSense\` folder.
Restart the game to pick up changes. No `.pck` is needed; the mod is code-only.

`Directory.Build.props` (git-ignored) holds the machine-specific MegaDot path used only by Publish.
