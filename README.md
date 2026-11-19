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

## Если Powershell не установился автоматически
```bash
dotnet tool restore
```
