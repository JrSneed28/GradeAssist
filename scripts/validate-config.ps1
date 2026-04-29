<#
.SYNOPSIS
    Validates all GradeAssist config files against their schemas and business rules.

.DESCRIPTION
    Discovers config/*.json files (excluding schemas) and validates each against
    the corresponding ConfigValidator methods in GradeAssist.Core.
    Exits with code 1 if any config fails validation.

.EXAMPLE
    pwsh -ExecutionPolicy RemoteSigned -File .\scripts\validate-config.ps1
#>
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$configDir = Join-Path $repoRoot "config"

$requiredConfigs = @(
    "sample-machine.json",
    "sample-grade-target.json",
    "safety-policy.json",
    "assist-tuning.json",
    "keybinds.json",
    "unity-settings.json",
    "MountProfiles.sample.json",
    "RigMaps.sample.json"
)

# Build Core if DLL not present
$coreDll = Join-Path $repoRoot "src\GradeAssist.Core\bin\Release\net8.0\GradeAssist.Core.dll"
if (-not (Test-Path $coreDll)) {
    Write-Host "Building GradeAssist.Core..."
    dotnet build (Join-Path $repoRoot "src\GradeAssist.Core\GradeAssist.Core.csproj") --configuration Release | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

# Add the Core assembly to the session (idempotent)
if (-not ([System.Management.Automation.PSTypeName]'GradeAssist.Core.ConfigValidator').Type) {
    try {
        Add-Type -Path $coreDll
    }
    catch {
        Write-Host "Warning: Could not load Core assembly. Attempting to continue..." -ForegroundColor Yellow
        throw
    }
}

$allPassed = $true

foreach ($fileName in $requiredConfigs) {
    $filePath = Join-Path $configDir $fileName
    if (-not (Test-Path $filePath)) {
        Write-Host "[MISSING] $fileName" -ForegroundColor Red
        $allPassed = $false
        continue
    }

    $json = Get-Content -Raw -Path $filePath
    $result = $null

    switch ($fileName) {
        "sample-machine.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateMachineJson($json)
        }
        "sample-grade-target.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateGradeTargetJson($json)
        }
        "safety-policy.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateSafetyPolicyJson($json)
        }
        "assist-tuning.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateAssistTuningJson($json)
        }
        "keybinds.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateKeybindsJson($json)
        }
        "unity-settings.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateUnitySettingsJson($json)
        }
        "MountProfiles.sample.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateMountProfilesJson($json)
        }
        "RigMaps.sample.json" {
            $result = [GradeAssist.Core.ConfigValidator]::ValidateRigMapsJson($json)
        }
        default {
            Write-Host "[SKIP] $fileName (unknown type)" -ForegroundColor Yellow
            $allPassed = $false
            continue
        }
    }

    if ($result -and $result.IsValid) {
        Write-Host "[PASS] $fileName" -ForegroundColor Green
    }
    else {
        Write-Host "[FAIL] $fileName : $($result.Message)" -ForegroundColor Red
        $allPassed = $false
    }
}

if (-not $allPassed) {
    throw "One or more config files failed validation."
}

Write-Host "All configs validated successfully." -ForegroundColor Green
