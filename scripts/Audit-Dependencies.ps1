<#
.SYNOPSIS
    Supply-chain audit of every NuGet dependency in this repository.

.DESCRIPTION
    Checks each project for:
      1. Known vulnerabilities, direct and transitive (GitHub Advisory Database, via NuGet).
      2. Deprecated or legacy packages.
      3. Floating version ranges ("*", "1.2.*"), which let a future restore pull an unreviewed
         version. Analyzers and source generators matter most here because they execute at build time.
      4. Missing lock files, which are what make a transitive dependency change visible in review.

    Exits non-zero if anything in categories 1-3 is found, so it can gate a pull request.

.EXAMPLE
    ./scripts/Audit-Dependencies.ps1
    ./scripts/Audit-Dependencies.ps1 -FailOnFloating:$false
#>
param(
    [string[]]$Projects = @(
        "SpireSense/SpireSense.csproj",
        "tests/SpireSense.Tests/SpireSense.Tests.csproj"
    ),
    [switch]$FailOnFloating = $true
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot

$problems = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Invoke-ListPackage {
    param([string]$Project, [string]$Mode)

    # --format json keeps this robust against wording changes in the CLI's text output.
    $raw = & dotnet list $Project package $Mode --include-transitive --format json 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet list package $Mode failed for ${Project}:`n$raw"
    }

    $start = $raw.IndexOf('{')
    if ($start -lt 0) { return $null }
    return $raw.Substring($start) | ConvertFrom-Json
}

function Read-Findings {
    param($Report, [string]$Label, [string]$Project)

    if ($null -eq $Report) { return }
    foreach ($framework in $Report.projects.frameworks) {
        foreach ($listName in @("topLevelPackages", "transitivePackages")) {
            foreach ($pkg in $framework.$listName) {
                $detail = ""
                if ($pkg.vulnerabilities) {
                    $detail = ($pkg.vulnerabilities | ForEach-Object { "$($_.severity): $($_.advisoryurl)" }) -join "; "
                } elseif ($pkg.deprecationReasons) {
                    $detail = ($pkg.deprecationReasons -join ", ")
                    if ($pkg.alternativePackage) { $detail += " (use $($pkg.alternativePackage.id))" }
                }
                $problems.Add("$Label  $Project  $($pkg.id) $($pkg.resolvedVersion)  $detail")
            }
        }
    }
}

Write-Host "Auditing $($Projects.Count) project(s) in $repoRoot" -ForegroundColor Cyan
Write-Host ""

foreach ($project in $Projects) {
    if (-not (Test-Path $project)) {
        $problems.Add("MISSING   $project not found")
        continue
    }

    Write-Host "== $project"

    # Restore first so the audit has a resolved graph to work from.
    & dotnet restore $project --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "restore failed for $project" }

    Read-Findings -Report (Invoke-ListPackage -Project $project -Mode "--vulnerable") -Label "VULNERABLE" -Project $project
    Read-Findings -Report (Invoke-ListPackage -Project $project -Mode "--deprecated") -Label "DEPRECATED" -Project $project

    # Floating versions defeat pinning: the next restore can silently take a different build.
    $content = Get-Content $project -Raw
    foreach ($m in [regex]::Matches($content, '<PackageReference\s+Include="([^"]+)"\s+Version="([^"]*\*[^"]*)"')) {
        $line = "FLOATING  $project  $($m.Groups[1].Value) = $($m.Groups[2].Value)"
        if ($FailOnFloating) { $problems.Add($line) } else { $warnings.Add($line) }
    }

    $lockFile = Join-Path (Split-Path $project -Parent) "packages.lock.json"
    if (-not (Test-Path $lockFile)) {
        $warnings.Add("NO LOCK   $project has no packages.lock.json; set RestorePackagesWithLockFile")
    }
}

Pop-Location

Write-Host ""
foreach ($w in $warnings) { Write-Host $w -ForegroundColor Yellow }

if ($problems.Count -gt 0) {
    Write-Host ""
    Write-Host "Dependency audit FAILED with $($problems.Count) finding(s):" -ForegroundColor Red
    foreach ($p in $problems) { Write-Host "  $p" -ForegroundColor Red }
    exit 1
}

Write-Host "Dependency audit passed: no vulnerable, deprecated or floating packages." -ForegroundColor Green
exit 0
