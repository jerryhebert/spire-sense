<#
.SYNOPSIS
    Decompiles the game's main assembly (sts2.dll) into reference/sts2-decompiled so the
    game's code can be searched and read while writing mods.

.DESCRIPTION
    Run this again after every game update. The output folder is git-ignored.
    Uses the small ILSpy-engine wrapper in scripts/decompiler (built on first run).
#>
param(
    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$OutRoot  = (Join-Path (Split-Path $PSScriptRoot -Parent) "reference")
)

$ErrorActionPreference = "Stop"
$dataDir = Join-Path $GamePath "data_sts2_windows_x86_64"
$dll     = Join-Path $dataDir "sts2.dll"
if (-not (Test-Path $dll)) { throw "sts2.dll not found at $dll" }

$release = Get-Content (Join-Path $GamePath "release_info.json") | ConvertFrom-Json
$out = Join-Path $OutRoot "sts2-decompiled"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force $out | Out-Null

Write-Host "Decompiling sts2.dll (game $($release.version), commit $($release.commit)) -> $out"
dotnet run --project (Join-Path $PSScriptRoot "decompiler\Sts2Decompiler.csproj") -c Release -- $dll $out
if ($LASTEXITCODE -ne 0) { throw "decompiler exited with $LASTEXITCODE" }

# Record which game build this reference corresponds to.
@{ version = $release.version; commit = $release.commit; date = $release.date; decompiled = (Get-Date -Format "o") } |
    ConvertTo-Json | Set-Content (Join-Path $out "DECOMPILED_FROM.json") -Encoding utf8

Write-Host "Done. Reference source is in $out"
