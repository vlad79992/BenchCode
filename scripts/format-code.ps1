param(
    [switch]$Check
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$java = Get-Command java -ErrorAction SilentlyContinue
if (-not $java) {
    throw "Java 21 or later is required to run ktfmt."
}

$sourceFiles = @(& git -C $repoRoot ls-files --cached --others --exclude-standard -- '*.kt')
if ($LASTEXITCODE -ne 0) {
    throw "Could not list Kotlin source files."
}

$solution = Join-Path $repoRoot "BenchCode.slnx"

Push-Location $repoRoot
try {
    foreach ($mode in @("style", "whitespace")) {
        $formatArguments = @("format", $solution, $mode, "--no-restore")
        if ($mode -eq "style") {
            $formatArguments += @("--severity", "warn", "--exclude-diagnostics", "IDE1006")
        }
        if ($Check) {
            $formatArguments += "--verify-no-changes"
        }

        & dotnet @formatArguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet format $mode failed."
        }
    }
} finally {
    Pop-Location
}

$csharpFiles = @(& git -C $repoRoot ls-files --cached --others --exclude-standard -- '*.cs')
if ($LASTEXITCODE -ne 0) {
    throw "Could not list C# source files."
}

$allSourceFiles = @($csharpFiles) + @($sourceFiles)
foreach ($file in $allSourceFiles) {
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines((Join-Path $repoRoot $file))) {
        $lineNumber++
        if ($line -match '^[ \t]*\t') {
            throw "$file`:$lineNumber uses a tab for indentation; use spaces."
        }
        if ($file.EndsWith(".cs", [StringComparison]::OrdinalIgnoreCase) -and $line.Length -gt 110) {
            throw "$file`:$lineNumber exceeds the 110-character line limit."
        }
    }
}

if ($sourceFiles.Count -eq 0) {
    exit 0
}

$ktfmtVersion = "0.64"
$ktfmtChecksum = "5b3d5286fd2defcc7dc8e28c21ddf156cc6b2d8682bdcd929ce4333e7a6201f2"
$cacheRoot = if ($IsWindows) {
    Join-Path $env:LOCALAPPDATA "BenchCode\ktfmt"
} elseif ($IsMacOS) {
    Join-Path $HOME "Library/Caches/BenchCode/ktfmt"
} else {
    Join-Path $HOME ".cache/benchcode/ktfmt"
}
$ktfmtJar = Join-Path $cacheRoot "ktfmt-$ktfmtVersion-with-dependencies.jar"

if (-not (Test-Path $ktfmtJar) -or (Get-FileHash $ktfmtJar -Algorithm SHA256).Hash -ne $ktfmtChecksum) {
    New-Item -ItemType Directory -Path $cacheRoot -Force | Out-Null
    $downloadPath = Join-Path $cacheRoot ("ktfmt-" + [Guid]::NewGuid().ToString("N") + ".download")
    try {
        $downloadUrl = "https://repo.maven.apache.org/maven2/com/facebook/ktfmt/$ktfmtVersion/ktfmt-$ktfmtVersion-with-dependencies.jar"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath
        if ((Get-FileHash $downloadPath -Algorithm SHA256).Hash -ne $ktfmtChecksum) {
            throw "The downloaded ktfmt archive failed its SHA-256 checksum verification."
        }

        Move-Item -Path $downloadPath -Destination $ktfmtJar -Force
    } finally {
        if (Test-Path $downloadPath) {
            Remove-Item -Path $downloadPath -Force
        }
    }
}

$ktfmtArguments = @("--enable-editorconfig", "--kotlinlang-style")
if ($Check) {
    $ktfmtArguments += @("--dry-run", "--set-exit-if-changed")
}
$ktfmtArguments += $sourceFiles

Push-Location $repoRoot
try {
    & $java.Source -jar $ktfmtJar @ktfmtArguments
    if ($LASTEXITCODE -ne 0) {
        throw "ktfmt reported Kotlin formatting issues."
    }
} finally {
    Pop-Location
}
