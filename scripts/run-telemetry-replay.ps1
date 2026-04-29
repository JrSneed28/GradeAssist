$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    # Build the replay app
    dotnet build .\src\GradeAssist.Replay\GradeAssist.Replay.csproj --configuration Release

    # Run the replay app against telemetry/samples
    dotnet run --project .\src\GradeAssist.Replay\GradeAssist.Replay.csproj --configuration Release -- "telemetry/samples" "artifacts/replay"

    # Verify outputs exist
    $mdFiles = Get-ChildItem -Path "artifacts/replay" -Filter "*.md"
    $csvFiles = Get-ChildItem -Path "artifacts/replay" -Filter "*.csv"

    if ($mdFiles.Count -eq 0) {
        throw "No markdown reports found in artifacts/replay"
    }
    if ($csvFiles.Count -eq 0) {
        throw "No CSV reports found in artifacts/replay"
    }

    Write-Host "Telemetry replay complete. Reports:"
    foreach ($f in ($mdFiles + $csvFiles)) {
        Write-Host "  $($f.Name)"
    }
}
finally {
    Pop-Location
}
