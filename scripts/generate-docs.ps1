param(
    [switch]$Clean,
    [switch]$BuildOnly,
    [switch]$NoOpen,
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $repoRoot "docfx.json"
$sitePath   = Join-Path $repoRoot "_site"
$apiPath    = Join-Path $repoRoot "api"

if (-not (Test-Path $configPath)) {
    Write-Error "Конфигурация DocFX не найдена по пути: $configPath"
    exit 1
}

if ($Clean) {
    if (Test-Path $sitePath) { Remove-Item -Path $sitePath -Recurse -Force }
    if (Test-Path $apiPath)  { Get-ChildItem -Path $apiPath -Filter "*.yml" | Remove-Item -Force }
}

& dotnet docfx $configPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Сборка документации завершилась с ошибкой (код: $LASTEXITCODE)."
    exit $LASTEXITCODE
}

if ($BuildOnly) {
    exit 0
}

$url = "http://localhost:$Port"
$serverProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "docfx", "serve", "`"$sitePath`"", "--port", $Port `
    -NoNewWindow `
    -PassThru

try {
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        if ($serverProcess.HasExited) {
            throw "Сервер DocFX неожиданно завершился (возможно, порт $Port уже занят)."
        }
        try {
            $null = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 1 -ErrorAction Stop
            $ready = $true
            break
        } catch {
            Start-Sleep -Milliseconds 200
        }
    }

    if (-not $ready) {
        throw "Сервер DocFX не ответил по адресу $url."
    }

    if (-not $NoOpen) {
        if ($IsMacOS) {
            Start-Process "open" -ArgumentList $url
        } else {
            Start-Process $url
        }
    }

    $serverProcess.WaitForExit()
}
finally {
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }
}
