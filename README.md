# Spire Sense

An in-run overlay for **Slay the Spire 2** that counts how many cards in your deck do each
deckbuilding "job", following the framework in
[Solving the Spire with Jobs](https://sts2.untapped.gg/en/articles/slay-the-spire-deckbuilding-strategy-solving-the-spire-with-jobs).

| Job | Meaning |
|---|---|
| Frontloaded Damage | Deals real damage the turn it is played, no setup needed |
| of which AoE | The subset of those that hit every enemy |
| Frontloaded Block | Prevents damage the turn it is played (block, weaken-all, intangible…) |
| Scaling | Makes the deck stronger as the fight goes on (powers, strength, engines) |
| Card Draw / Manipulation | Draw, scry, tutor, retain, top-decking, permanent thinning |

The panel appears once a run is in progress and updates as your deck changes. **F8** toggles it and
you can drag it anywhere; both are remembered. A card can count toward several jobs. Upgrades do not
change a card's jobs. Curses and statuses are counted separately and never contribute to a job.

**Hovering any card** adds a Spire Sense line to its tooltip naming the jobs it counts for, and says
when a classification was guessed rather than curated.

**Disagree with a verdict?** Open any card in the inspect view, from the Compendium or from a run,
and a panel appears with a toggle per job. Your choice is saved to
`%APPDATA%\SlayTheSpire2\spiresense_overrides.json` and takes priority over the shipped tables, so
it survives mod updates. Reset returns the card to the shipped classification.

All 519 cards across the Ironclad, Silent, Defect, Necrobinder, Regent and Colorless pools are
hand-classified, each with a one-line rationale. See [SpireSense/README.md](SpireSense/README.md) for
how classification works and how to change a verdict.

## Installing

Grab `SpireSense.dll` and `SpireSense.json` from a release, put them in a `mods/SpireSense/` folder
inside your Slay the Spire 2 install directory, and restart the game. The mod is code-only: there is
no `.pck` and no dependency on other mods. It declares `affects_gameplay: false`, so it does not
trigger multiplayer mod-mismatch checks.

## Building

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download) and a copy of the game, whose
assemblies the mod compiles against. The project finds a Steam install automatically; set `Sts2Path`
in `SpireSense/Directory.Build.props` if yours lives elsewhere.

```bash
dotnet build SpireSense/SpireSense.csproj -c Release
```

Building copies the mod straight into the game's `mods/SpireSense/` folder. Restart the game to pick
up changes; mods cannot hot-reload. Launching the game with the `nomods` argument gives you a vanilla
run for comparison.

## Checks

```bash
pwsh ./scripts/Run-Checks.ps1
```

Runs the unit tests, the dependency audit, and a Release build. Add `-SkipBuild` on a machine without
the game installed.

**Tests.** The decision-making code has no dependency on the game or on Godot: `CardFacts` is a plain
snapshot of what a card contributes, and one adapter file reads that out of the game's model. The test
project compiles those source files directly rather than referencing the mod project, because the mod
project links the game's assemblies and cannot load outside the game. That is also what lets tests run
in CI, where no copy of the game exists. The most important tests guard the classification data
itself: full pool coverage, no card in two tables, every AoE card also counted as damage, every job
name valid.

**Dependency audit.** `scripts/Audit-Dependencies.ps1` checks every direct and transitive NuGet
package for known vulnerabilities and deprecation, and rejects floating version ranges such as
`Version="*"` that would let a future restore pull an unreviewed version. Build-time dependencies like
analyzers matter most, since they execute code during every build. Restore-time auditing is enabled in
both project files. The test project also commits a `packages.lock.json`, so a change to any
transitive dependency shows up in the diff; the mod project deliberately does not, because the Godot
SDK resolves a different graph per build configuration and NuGet would rewrite the file constantly.

**What CI cannot do.** It cannot compile the mod. That needs the game's own assemblies, which are not
redistributable and are deliberately absent from this repository. CI restores the mod project in
locked mode to catch dependency drift; compiling stays a local step.

## Repository layout

| Path | Purpose | In git? |
|---|---|---|
| `SpireSense/` | The mod. Solution and project share a directory, which Godot requires. | yes |
| `tests/` | Unit tests. No dependency on the game, so they run anywhere. | yes |
| `scripts/` | Check runner, dependency audit, and the decompiler used for reference. | yes |
| `.github/` | CI workflow and Dependabot config. | yes |
| `reference/` | The game's own code, decompiled locally for reference. | **no** |
| `tools/` | MegaDot editor and export templates, downloaded locally. | **no** |

`reference/` and `tools/` are deliberately git-ignored. Regenerate the reference source yourself with
`scripts/Decompile-Sts2.ps1` after any game update.

## Development environment

- **.NET 9 SDK** — required to build anything.
- **An IDE** — JetBrains Rider is what the community mod templates target; anything that reads `.sln`
  works.
- **A .NET decompiler** — ILSpy, for reading the game's code. `scripts/Decompile-Sts2.ps1` produces a
  searchable copy of the whole assembly under `reference/`.
- **MegaDot** — MegaCrit's Godot fork, from <https://megadot.megacrit.com/>. Only needed for mods that
  ship assets in a `.pck`. Spire Sense does not, so this is optional here. The game refuses `.pck`
  files exported by a newer Godot than it runs.
- **Mod templates** — `dotnet new install Alchyr.Sts2.Templates`, documented at
  <https://github.com/Alchyr/ModTemplate-StS2/wiki>.

## How the game loads mods

Derived from the game's own `ModManager`:

- At startup the game scans `<game>/mods/` recursively, plus Steam Workshop items, for `*.json`
  manifests.
- For a manifest with `"id": "Foo"` it loads `Foo.dll` if `has_dll` and `Foo.pck` if `has_pck` from the
  same folder. File names must match the id.
- Dependencies are topologically sorted and must be present and loaded.
- If a type in the dll carries `[ModInitializer("MethodName")]`, that static method is called.
  Otherwise the game creates a Harmony instance and calls `PatchAll` on the assembly.
- `affects_gameplay: false` marks cosmetic mods so multiplayer does not compare them.
- Mods cannot be reloaded at runtime.

## After a game update

1. Check `release_info.json` in the game folder for the new version.
2. Run `scripts/Decompile-Sts2.ps1` to refresh the reference source.
3. Rebuild and fix anything whose signatures changed. Bump `min_game_version` in the manifest if the
   mod now relies on the newer build.

## License and attribution

[MIT](LICENSE). Slay the Spire 2 is the property of Mega Crit; this project is an unaffiliated fan mod
and contains no game code or assets. The project was scaffolded from
[Alchyr's mod template](https://github.com/Alchyr/ModTemplate-StS2).
