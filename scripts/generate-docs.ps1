$sitePath = "./_site"

dotnet docfx docfx.json
$indexFile = Join-Path $sitePath "index.html"

if (Test-Path $indexFile) {
    Invoke-Item $indexFile
} else {
    Write-Error "Файл документации не найден по пути: $indexFile"
}
