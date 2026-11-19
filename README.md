# BenchCode

# Документация

Документация находится по этой ссылке

https://vlad79992.github.io/BenchCode

## Скрипты

> [!NOTE] 
> команды выполняются из корня проекта

## Генерация документации:

*открывает статические файлы в браузере*
```bash
dotnet pwsh ./scripts/generate-docs.ps1
```

### или 
*поднимает сервер для просмотра документации*
```bash
dotnet pwsh ./scripts/generate-docs.ps1 -Serve
```

## Генерация отчета о тестах:
```bash
dotnet pwsh ./scripts/generate-test-report.ps1
```

## Форматирование кода

Для C# используется `dotnet format` с настройками из `.editorconfig`, для Kotlin — `ktfmt` с настройками по умолчанию.

Для форматирования обоих языков из корня репозитория выполните:

```bash
dotnet restore BenchCode.slnx
dotnet tool restore
dotnet pwsh ./scripts/format-code.ps1
```

Для проверки C# без изменения файлов:

```bash
dotnet format --verify-no-changes --verbosity detailed
```

Для проверки C# и Kotlin используется команда:

```bash
dotnet pwsh ./scripts/format-code.ps1 -Check
```

Workflow `Code style` запускает эту проверку для pull request и push в `master`; она не изменяет файлы. Чтобы сделать её обязательной для слияния, включите branch protection/ruleset для `master` и добавьте `Code style / format` в required status checks.

`ktfmt` запускается без дополнительных style-флагов; для Kotlin в GitHub Actions используется JDK 21.

## Если Powershell не установился автоматически
```bash
dotnet tool restore
```
