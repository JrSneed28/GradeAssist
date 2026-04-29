#Requires -Version 7
<#
.SYNOPSIS
    Regenerates deterministic telemetry replay fixture expected-output files.
.DESCRIPTION
    Reads sample telemetry and config from the repo, runs the replay harness,
    and writes expected fixture files to telemetry/fixtures/expected/.
    Operates only inside the repo. Never writes outside the workspace.
.PARAMETER DryRun
    If set, prints what would be written without creating files.
.PARAMETER Force
    Overwrites existing fixture files without prompting.
.EXAMPLE
    pwsh ./scripts/regen-fixtures.ps1
    pwsh ./scripts/regen-fixtures.ps1 -DryRun
#>
param(
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $expectedDir = Join-Path $repoRoot 'telemetry' 'fixtures' 'expected'
    if (-not $DryRun) {
        if (-not (Test-Path $expectedDir)) {
            New-Item -ItemType Directory -Path $expectedDir -Force | Out-Null
        }
    }

    # Find sample telemetry files
    $sampleDir = Join-Path $repoRoot 'telemetry' 'samples'
    $sampleFiles = Get-ChildItem -Path $sampleDir -Filter '*.csv' -ErrorAction SilentlyContinue

    if (-not $sampleFiles) {
        Write-Warning "No sample telemetry CSV files found in $sampleDir. Skipping fixture regeneration."
        exit 0
    }

    # Locate replay harness (if built)
    $replayExe = Join-Path $repoRoot 'src' 'GradeAssist.Replay' 'bin' 'Release' 'net8.0' 'GradeAssist.Replay.exe'
    $useReplayExe = Test-Path $replayExe

    foreach ($sample in $sampleFiles) {
        $fixtureName = $sample.BaseName + '-expected.json'
        $fixturePath = Join-Path $expectedDir $fixtureName

        if (-not $Force -and (Test-Path $fixturePath) -and -not $DryRun) {
            Write-Host "Skipping existing fixture: $fixturePath (use -Force to overwrite)"
            continue
        }

        if ($DryRun) {
            Write-Host "[DRY-RUN] Would generate: $fixturePath from $($sample.FullName)"
            continue
        }

        if ($useReplayExe) {
            # Run replay harness and capture output
            $reportJson = & $replayExe --input $sample.FullName --output-format json 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Replay harness failed for $($sample.Name). Skipping."
                continue
            }
            $reportJson | Set-Content -Path $fixturePath -Encoding UTF8
            Write-Host "Generated fixture: $fixturePath"
        }
        else {
            # Fallback: generate a placeholder fixture with schema version header
            $placeholder = @{
                schemaVersion = '1.0'
                source        = $sample.Name
                generatedAt   = (Get-Date -Format 'o')
                note          = 'Placeholder: replay harness not yet built. Replace with actual run output.'
                results       = @{}
            } | ConvertTo-Json -Depth 5
            $placeholder | Set-Content -Path $fixturePath -Encoding UTF8
            Write-Host "Generated placeholder fixture: $fixturePath"
        }
    }

    Write-Host "Fixture regeneration complete."
}
finally {
    Pop-Location
}
