# Spire Sense

An in-run overlay for Slay the Spire 2 that counts how many cards in your deck do each deckbuilding
"job", following the framework in
[Solving the Spire with Jobs](https://sts2.untapped.gg/en/articles/slay-the-spire-deckbuilding-strategy-solving-the-spire-with-jobs):

| Job | Meaning |
|---|---|
| Frontloaded Damage | Deals real damage the turn it is played, no setup needed |
| of which AoE | The subset of those that hit every enemy |
| Frontloaded Block | Prevents damage the turn it is played (block, weaken-all, intangible...) |
| Scaling | Makes the deck stronger as the fight goes on (powers, strength, engines) |
| Card Draw / Manipulation | Draw, scry, tutor, retain, top-decking, permanent thinning |

A card can count toward several jobs. Upgrades do not change a card's jobs. Curses and statuses are
counted separately and never contribute to a job.

## Using it

- The panel appears automatically once a run is in progress and hides on the main menu.
- **F8** toggles it. Click and drag the panel to move it. Both are remembered.
- Settings live in `%APPDATA%\SlayTheSpire2\spiresense.json` (created on first toggle or drag):
  `visible`, `x`, `y`, `font_size`, `toggle_key` (any Godot `Key` name, e.g. `F8`, `Backslash`, `KpMultiply`), `show_card_names`.
- Cards the mod has no table entry for are classified by a rough heuristic and shown as
  "(N guessed)" plus a "Guessed:" list. Cards where even the heuristic finds nothing appear under "No job:".

## How classification works

- `SpireSenseCode/Data/jobs.<pool>.json` holds one curated entry per card, keyed by the card's C# class
  name in the game assembly (e.g. `PommelStrike`). Each entry has a `jobs` array and a `note`.
  These files are embedded into the dll at build time, so edit and rebuild to change a verdict.
- `Jobs/CardClassifier.cs` applies the table first and falls back to a heuristic (Power => Scaling,
  Attack with base damage => Frontloaded Damage, AllEnemies target => AoE, block => Frontloaded Block,
  Cards var => Card Draw, self Strength/Dexterity => Scaling).
- The tables were produced by reading each card's decompiled implementation against the job
  definitions above. Judgment calls are recorded in each card's `note`.

## Building

Open `SpireSense.sln` in Rider and Build, or run `dotnet build -c Release` in this folder.
The build copies `SpireSense.dll`, `.pdb` and `.json` into the game's `mods\SpireSense\` folder.
Restart the game to pick up changes. No `.pck` is needed; the mod is code-only.

`Directory.Build.props` (git-ignored) holds the machine-specific MegaDot path used only by Publish.
