$projectPath = "./BenchCode.Test/BenchCode.Test.csproj"
$resultsDir = "./BenchCode.Test/TestResults"
$reportDir = "./CoverageReport"

if (Test-Path $resultsDir) { 
    Remove-Item -Path $resultsDir -Recurse -Force 
}
if (Test-Path $reportDir) { 
    Remove-Item -Path $reportDir -Recurse -Force 
}

dotnet test $projectPath --collect:"XPlat Code Coverage" --results-directory $resultsDir --nologo

dotnet reportgenerator `
    -reports:"$resultsDir/**/coverage.cobertura.xml" `
    -targetdir:"$reportDir" `
    -reporttypes:Html

$indexFile = Join-Path $reportDir "index.html"
if (Test-Path $indexFile) {
    Invoke-Item $indexFile
} else {
    Write-Error "Файл отчета не найден по пути: $indexFile"
}
