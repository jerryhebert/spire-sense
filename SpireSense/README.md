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

## Using it

- The panel shows on the combat screen, and on the screens where you choose a card to add to your
  deck: the reward after a fight, and the card or bundle offered by Neow, an event or a shop. Those
  are the moments the counts inform a decision.
- It hides everywhere else: the settings menu, the map, your draw or discard pile, the deck view.
  It asks the game which screen is topmost rather than keeping its own list, so screens added by a
  future game update hide it correctly without a change here.
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
- Below a divider, a **Cycle** section giving damage and mitigation over one whole pass through the
  deck. Cycle length is deck size divided by cards drawn per turn, fractionally: 15 cards at 5 draw
  is 3 turns, 15 cards at 7 draw is 15/7. The draw figure is read from the game, so relics and
  powers that change it shorten or lengthen the cycle correctly.
  - Note what a whole-cycle total does and does not show. Over one pass you still play every real
    card, so curses do not lower it; what they cost you is turns. Cycle length going up is the
    signal that the deck has slowed down.
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
