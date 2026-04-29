$ErrorActionPreference = "Continue"

function Check-Command($name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($null -eq $cmd) {
        Write-Host "[MISSING] $name" -ForegroundColor Red
        return $false
    }
    Write-Host "[OK] $name -> $($cmd.Source)" -ForegroundColor Green
    return $true
}

$ok = $true
$ok = (Check-Command "git") -and $ok
$ok = (Check-Command "dotnet") -and $ok
$ok = (Check-Command "pwsh") -and $ok

# Optional tools for later milestones.
Check-Command "code" | Out-Null
Check-Command "blender" | Out-Null
Check-Command "ffmpeg" | Out-Null

if (-not $ok) {
    Write-Error "One or more required core tools are missing from PATH."
    exit 1
}

Write-Host "Core tools are visible." -ForegroundColor Green
