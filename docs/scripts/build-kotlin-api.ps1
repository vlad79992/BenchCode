$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$composeRoot = Join-Path $repositoryRoot 'BenchCode.ComposeWebApp'
$dokkaRoot = Join-Path $composeRoot 'dokka'
$dependencyDirectory = Join-Path $dokkaRoot 'target\dependency'
$generatedConfigPath = Join-Path $dokkaRoot 'target\dokka.generated.json'
$pomPath = Join-Path $dokkaRoot 'pom.xml'
$cliVersion = '2.2.0'

& mvn --batch-mode --quiet --file $pomPath 'org.apache.maven.plugins:maven-dependency-plugin:3.8.1:copy-dependencies' '-DincludeScope=runtime' "-DoutputDirectory=$dependencyDirectory"
if ($LASTEXITCODE -ne 0) {
    throw "Maven failed to resolve Dokka CLI dependencies (exit code $LASTEXITCODE)."
}

$cliJar = Join-Path $dependencyDirectory "dokka-cli-$cliVersion.jar"
if (-not (Test-Path $cliJar)) {
    throw "Dokka CLI jar was not found at '$cliJar'. Check the Maven dependency output."
}

$pluginJars = @(
    Get-ChildItem -Path $dependencyDirectory -Filter '*.jar' |
        Where-Object { $_.Name -ne "dokka-cli-$cliVersion.jar" } |
        ForEach-Object { $_.FullName }
)
if ($pluginJars.Count -eq 0) {
    throw "No Dokka plugin dependencies were resolved to '$dependencyDirectory'."
}

$config = Get-Content -Path (Join-Path $dokkaRoot 'dokka.json') -Raw | ConvertFrom-Json
$config.outputDir = Join-Path $repositoryRoot '.dokka\kotlin-api'
$config | Add-Member -MemberType NoteProperty -Name pluginsClasspath -Value $pluginJars -Force
foreach ($sourceSet in $config.sourceSets) {
    $sourceSet.sourceRoots = @(
        $sourceSet.sourceRoots | ForEach-Object {
            Join-Path $composeRoot $_
        }
    )
}

New-Item -ItemType Directory -Force -Path (Split-Path $generatedConfigPath) | Out-Null
$json = ConvertTo-Json -InputObject $config -Depth 100
[System.IO.File]::WriteAllText(
    $generatedConfigPath,
    $json,
    [System.Text.UTF8Encoding]::new($false)
)

& java -jar $cliJar $generatedConfigPath
if ($LASTEXITCODE -ne 0) {
    throw "Dokka CLI failed (exit code $LASTEXITCODE)."
}

$indexPath = Join-Path $config.outputDir 'index.html'
if (-not (Test-Path $indexPath)) {
    throw "Dokka completed without producing the expected HTML entry point '$indexPath'."
}
