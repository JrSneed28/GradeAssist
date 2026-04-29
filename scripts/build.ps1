$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    dotnet build .\src\GradeAssist.Core\GradeAssist.Core.csproj --configuration Release
    dotnet build .\src\GradeAssist.Tests\GradeAssist.Tests.csproj --configuration Release
}
finally {
    Pop-Location
}
