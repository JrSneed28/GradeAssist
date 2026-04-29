$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    # ─── Pre-flight verification ───
    pwsh .\scripts\final-verify.ps1

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $releaseDir = ".\10_releases_local"
    $stage = ".\release-staging\GradeAssist-$stamp"
    $zipName = "GradeAssist-$stamp.zip"
    $zipPath = Join-Path $releaseDir $zipName
    $manifestPath = Join-Path $releaseDir "GradeAssist-$stamp.manifest.json"

    New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    # ─── Top-level items to include ───
    $include = @("assets", "config", "docs", "scripts", "src", "telemetry", "artifacts", "CLAUDE.md", "README.md", "START_HERE.md", ".gitignore")
    foreach ($item in $include) {
        if (Test-Path $item) {
            Copy-Item $item $stage -Recurse -Force
        }
    }

    # ─── Exclusions ───
    $excludePatterns = @(
        "bin",
        "obj",
        ".vs",
        "TestResults",
        "Library",
        "Temp",
        "Logs",
        ".env",
        "*.env",
        "secrets.json",
        "*.pfx",
        "*.key",
        ".claude",
        ".claude-plugin",
        ".omc",
        "*.dll",
        "*.exe",
        "BepInEx*",
        "*_Data",
        "MonoBleedingEdge*"
    )

    foreach ($pattern in $excludePatterns) {
        Get-ChildItem -Path $stage -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object {
                ($_.Name -like $pattern) -or
                ($_.PSIsContainer -and ($_.Name -eq $pattern -or $_.Name -like $pattern))
            } |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    # ─── Build manifest ───
    $fileEntries = @()
    $allFiles = Get-ChildItem -Path $stage -Recurse -File -Force
    foreach ($file in $allFiles) {
        $relativePath = $file.FullName.Substring((Resolve-Path $stage).Path.Length + 1).Replace("\", "/")
        $sha256 = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
        $fileEntries += [PSCustomObject]@{
            relativePath = $relativePath
            sha256       = $sha256
        }
    }

    # ─── ZIP packaging ───
    Compress-Archive -Path "$stage\*" -DestinationPath $zipPath -Force
    $zipSha256 = (Get-FileHash $zipPath -Algorithm SHA256).Hash

    # ─── Write manifest JSON ───
    $manifest = [PSCustomObject]@{
        releaseStamp = $stamp
        fileCount    = $fileEntries.Count
        zipSha256    = $zipSha256
        files        = $fileEntries
    }
    $manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8

    # ─── Update ReleaseNotes.md ───
    $releaseNotesPath = ".\docs\ReleaseNotes.md"
    if (Test-Path $releaseNotesPath) {
        $existing = Get-Content $releaseNotesPath -Raw
        $releaseEntry = @"

---

## Release $stamp

- **ZIP:** $zipName
- **ZIP SHA256:** ``$zipSha256``
- **Files included:** $($fileEntries.Count)
- **Manifest:** $(Split-Path $manifestPath -Leaf)
"@
        $existing + $releaseEntry | Set-Content -Path $releaseNotesPath -Encoding UTF8
    }

    # ─── Cleanup staging ───
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host "Release created successfully." -ForegroundColor Green
    Write-Host "  ZIP:      $zipPath"
    Write-Host "  Manifest: $manifestPath"
    Write-Host "  Files:    $($fileEntries.Count)"
}
finally {
    Pop-Location
}
