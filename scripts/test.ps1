$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot\..
try {
    dotnet test .\src\GradeAssist.Tests\GradeAssist.Tests.csproj --configuration Release --collect:"XPlat Code Coverage"
}
finally {
    Pop-Location
}
