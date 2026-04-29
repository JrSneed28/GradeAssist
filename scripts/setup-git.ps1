$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    if (!(Test-Path .git)) {
        git init
    }
    git add .
    git status
    Write-Host "Git initialized/staged. Commit when ready:" -ForegroundColor Green
    Write-Host "git commit -m 'Initialize safe external grade assist prototype'"
}
finally {
    Pop-Location
}
