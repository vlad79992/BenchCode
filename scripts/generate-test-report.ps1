param(
    [string]$Filter = "",
    [switch]$NoOpen
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "../BenchCode.Test/BenchCode.Test.csproj"
$resultsDir  = Join-Path $PSScriptRoot "../BenchCode.Test/TestResults"
$reportDir   = Join-Path $PSScriptRoot "../CoverageReport"

if (Test-Path $resultsDir) { Remove-Item -Path $resultsDir -Recurse -Force }
if (Test-Path $reportDir)  { Remove-Item -Path $reportDir -Recurse -Force }

$testArgs = @(
    "test", $projectPath,
    "--collect:XPlat Code Coverage",
    "--results-directory", $resultsDir,
    "--nologo"
)

if ($Filter) {
    $testArgs += @("--filter", $Filter)
}

& dotnet $testArgs

$assemblyFilters = "-*Test*"
$fileFilters     = "-*Migrations*;-*.g.cs;-*.Designer.cs"

dotnet reportgenerator `
    -reports:"$resultsDir/**/coverage.cobertura.xml" `
    -targetdir:"$reportDir" `
    -reporttypes:"Html;TextSummary" `
    -assemblyfilters:"$assemblyFilters" `
    -filefilters:"$fileFilters" `
    -verbosity:Warning

$summaryFile = Join-Path $reportDir "Summary.txt"
if (Test-Path $summaryFile) {
    Get-Content $summaryFile | Select-String -Pattern "Line coverage:|Branch coverage:"
}

$indexFile = Join-Path $reportDir "index.html"
if (Test-Path $indexFile) {
    if (-not $NoOpen) {
        Invoke-Item $indexFile
    }
} else {
    Write-Error "Файл отчета не найден по пути: $indexFile"
}
