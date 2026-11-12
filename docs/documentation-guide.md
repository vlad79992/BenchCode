# Руководство по документации

## Общие страницы DocFX

Markdown-страницы находятся в `docs/`, а навигация задаётся в `docs/toc.yml`. Чтобы добавить страницу:

1. Создайте `docs/<page>.md` или файл в тематическом подкаталоге.
2. Добавьте страницу в `docs/toc.yml` с `name` и относительным `href`.
3. Если документ описывает архитектуру или команды, сверьте текст с актуальными `module.yaml`, исходниками и README проекта.
4. Для схем используйте Mermaid fenced blocks. DocFX включён с шаблонами `default` и `modern`, который отображает Mermaid.

Конфигурация `docfx.json` сопоставляет `docs/` с корнем сайта и собирает Dokka HTML как статический ресурс под `kotlin-api/`. Базовая `metadata`-секция подготовлена для будущего .NET API; workflow сейчас выполняет только `docfx build` и не генерирует .NET-контент.

## Обновление Kotlin API через Dokka CLI

Kotlin-проект использует Amper, поэтому API генерируется Dokka CLI, не Gradle-плагином. `BenchCode.ComposeWebApp/dokka/dokka.json` ограничивает вход production Kotlin-каталогами приложения: common, Android, JVM и Wasm JS. Тестовые source sets не включены. В `BenchCode.ComposeWebApp/dokka/pom.xml` закреплены CLI и необходимые HTML-плагины; Maven разрешает их транзитивные JAR-файлы.

Требования для API-генерации: Java 21 и Maven. Из корня репозитория запустите:

```powershell
pwsh -File docs/scripts/build-kotlin-api.ps1
```

На Windows PowerShell можно использовать:

```powershell
.\docs\scripts\build-kotlin-api.ps1
```

Скрипт загружает зависимости через Maven, подставляет абсолютные source roots и classpath в рабочую Dokka-конфигурацию, затем вызывает CLI. Результат создаётся в `.dokka/kotlin-api/`; не редактируйте этот генерируемый каталог вручную.

## Локальная сборка сайта

Нужны Java 21 и .NET 10 SDK. В корне репозитория восстановите закреплённый DocFX tool и последовательно соберите API, затем сайт:

```powershell
dotnet tool restore
pwsh -File docs/scripts/build-kotlin-api.ps1
dotnet tool run docfx build docfx.json
```

В Windows PowerShell замените `pwsh` на `powershell` для запуска скрипта. В результате появится `_site/`; откройте `_site/index.html` локально или обслужите каталог любым статическим HTTP-сервером. Сборка DocFX должна происходить после Dokka: HTML API является ресурсом-источником, который DocFX копирует в `_site/kotlin-api/`.

## GitHub Actions

`.github/workflows/docs.yml` запускается для pull request в `main` или `aspire`, push в эти ветки и вручную:

1. Восстанавливает DocFX из `.config/dotnet-tools.json`, настраивает Java 21 и Maven cache.
2. Запускает `build-kotlin-api.ps1`; Dokka CLI создаёт HTML API.
3. Запускает `docfx build docfx.json`, собирая общие страницы и копируя API в `kotlin-api/`.
4. Загружает весь `_site/` как GitHub Pages artifact. При pull request сборка проверяется, но публикация не выполняется; push в `aspire` развёртывает собранную с этой ветки версию сайта.

Публикация требует включённых GitHub Pages и разрешений Actions для Pages deployment. Путь до `kotlin-api/index.html` относительный, поэтому остаётся работоспособным при публикации репозитория как project site.
