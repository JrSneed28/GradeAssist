$ErrorActionPreference = "Continue"

$files = @(
    'C:\Users\bayba\Desktop\GradeAssist\src\GradeAssist.Core\GradePlane.cs',
    'C:\Users\bayba\Desktop\GradeAssist\src\GradeAssist.Core\GradeError.cs',
    'C:\Users\bayba\Desktop\GradeAssist\src\GradeAssist.Core\GradeTargetSettings.cs',
    'C:\Users\bayba\Desktop\GradeAssist\src\GradeAssist.Core\Vector3D.cs',
    'C:\Users\bayba\Desktop\GradeAssist\src\GradeAssist.Tests\GradeAssist.Tests.csproj'
)

foreach ($f in $files) {
    if (Test-Path $f) {
        Write-Output "PASS: $f"
    } else {
        Write-Output "FAIL: $f"
    }
}