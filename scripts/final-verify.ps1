$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    pwsh .\scripts\verify-tools.ps1
    pwsh .\scripts\build.ps1
    pwsh .\scripts\test.ps1
    Write-Host "Final verification passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
