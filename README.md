# Slay the Spire 2 modding workspace

Everything needed to build, package and install mods for Slay the Spire 2 on this machine.

## Layout

| Path | Purpose | In git? |
|---|---|---|
| `<ModName>/` | One folder per mod. Solution and project live in the same directory (Godot requires this). | yes |
| `scripts/Decompile-Sts2.ps1` | Regenerates `reference/sts2-decompiled` from the installed game. Re-run after every game update. | yes |
| `scripts/decompiler/` | Small console app around the ILSpy decompiler engine, used by the script above. | yes |
| `reference/sts2-decompiled/` | The game's C# source, decompiled. Read-only reference for finding hooks, models and IDs. `DECOMPILED_FROM.json` records the game build. | no |
| `tools/megadot/` | MegaDot (MegaCrit's Godot fork) editor and its export templates. Used to export `.pck` asset packs. | no |

## Toolchain

| Tool | Version | Location |
|---|---|---|
| Slay the Spire 2 | v0.111.0 (commit 41cef1ea, 2026-08-13) | `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` |
| Game runtime | .NET 9 (`net9.0`), MegaDot 4.5.1 | `data_sts2_windows_x86_64\` next to the exe |
| .NET SDK | 9.0.318 | `C:\Program Files\dotnet` |
| MegaDot editor | 4.5.1-m.14 | `tools\megadot\4.5.1-m.14\MegaDot_v4.5.1-stable_mono_win64.exe` |
| MegaDot export templates | 4.5.1.m.14.mono | `%APPDATA%\Godot\export_templates\4.5.1.m.14.mono\` |
| Alchyr.Sts2.Templates | 2.5.2 | `dotnet new alchyrsts2mod`, `alchyrsts2contentmod`, `alchyrsts2charmod` |
| BaseLib | v3.4.7 | Installed as a mod in the game's `mods\BaseLib\` folder; referenced by NuGet `Alchyr.Sts2.BaseLib` at build time |
| JetBrains Rider | 2026.2 | IDE |
| ILSpy | 11.0 | GUI decompiler, `%LOCALAPPDATA%\Programs\ILSpy` |
| Git | 2.55 | `C:\Program Files\Git` |

Re-download MegaDot from <https://megadot.megacrit.com/> (Windows `editor-csharp` zip plus the `mono-export-templates.tpz`). The `.tpz` is a zip; its `templates/` contents go in the export templates folder above, named after the engine's version string.

BaseLib releases: <https://github.com/Alchyr/BaseLib-StS2/releases>. Template docs: <https://github.com/Alchyr/ModTemplate-StS2/wiki>.

## How the game loads mods

Derived from `reference/sts2-decompiled/MegaCrit/Sts2/Core/Modding/ModManager.cs`.

- At startup the game scans `<game>\mods\` (recursively, subfolders allowed) and Steam Workshop items for `*.json` manifests.
- For a manifest with `"id": "Foo"` it loads `Foo.dll` if `has_dll` and `Foo.pck` if `has_pck` from the same folder. File names must match the id.
- Dependencies (`dependencies: [{ "id", "min_version" }]`) are topologically sorted and must be present and loaded.
- If a type in the dll carries `[ModInitializer("MethodName")]`, that static method is called. Otherwise the game creates a Harmony instance and calls `PatchAll` on the assembly.
- `affects_gameplay: false` marks cosmetic mods so multiplayer does not compare them.
- Launching the game with the `nomods` argument skips all mod loading (handy for vanilla comparisons).
- Mods cannot be reloaded at runtime. Restart the game after every build.

## Day-to-day workflow

1. Open `<ModName>\<ModName>.sln` in Rider.
2. **Build** compiles the dll and copies `<ModName>.dll`, `.pdb` and `.json` into `<game>\mods\<ModName>\`. Enough for code-only changes.
3. **Publish** (Rider: right-click project, Publish to folder; or `dotnet publish -c Release`) additionally runs MegaDot headless to export `<ModName>.pck`. Required after any change to assets, scenes, or localization files.
4. Launch the game, check the Mods screen, and read `%APPDATA%\SlayTheSpire2\logs\godot.log` for `[INFO]`/`[ERROR]` lines tagged with the mod id.

## Creating a new mod

```powershell
cd C:\Users\jerry\Projects\STS2Modding
dotnet new alchyrsts2mod -n MyMod -o MyMod --ModAuthor jerry   # or alchyrsts2contentmod / alchyrsts2charmod
dotnet new sln -n MyMod -o MyMod
dotnet sln MyMod\MyMod.sln add MyMod\MyMod.csproj
```

Then set `<GodotPath>` in `MyMod\Directory.Build.props` to the MegaDot exe path above. The project auto-detects the game install through the Steam registry keys.

## After a game update

1. Check `release_info.json` in the game folder for the new version.
2. Run `scripts\Decompile-Sts2.ps1` to refresh the reference source.
3. Rebuild each mod and fix anything whose signatures changed. Bump `min_game_version` in the manifest if the mod relies on the new build.
4. If MegaDot has a new release, update it too. The game refuses `.pck` files exported by a newer Godot than it runs.
