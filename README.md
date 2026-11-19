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

## Форматирование кода и проверка стиля

Для C# используются соглашения Microsoft из [документации C#](https://learn.microsoft.com/ru-ru/dotnet/csharp/fundamentals/coding-style/coding-conventions), проверяемые `dotnet format` и настроенные в `.editorconfig`; длина строки проекта ограничена 110 символами. Для Kotlin используется `ktfmt` в стиле [официальных соглашений Kotlin](https://kotlinlang.org/docs/coding-conventions.html). В обоих языках используются отступы в 4 пробела. `ktfmt` скачивается при первом запуске и сохраняется в пользовательском кэше.

Для Kotlin нужен Java 21 или новее.

Затем из корня репозитория выполните:

```bash
dotnet restore BenchCode.slnx
dotnet tool restore
dotnet pwsh ./scripts/format-code.ps1
```

Для проверки форматирования без изменения файлов:

```bash
dotnet pwsh ./scripts/format-code.ps1 -Check
```

Workflow `Code style` автоматически запускает эту проверку для pull request и push в `master`. Чтобы сделать успешную проверку обязательной для слияния, включите branch protection/ruleset для `master` в настройках репозитория GitHub и добавьте `Code style / format` в обязательные status checks. Проверка появится в списке после первого запуска workflow.

## Если Powershell не установился автоматически
```bash
dotnet tool restore
```
