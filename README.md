# BenchCode

# Документация

Документация находится по этой ссылке

https://vlad79992.github.io/BenchCode

> [!IMPORTANT]
> Перед использованием ендпоинтов для компиляции и запуска кода необходимо скачать образы контейнеров.
> [Подробнее в пункте "компиляция запуск кода"](#компиляция-и-запуск-кода-в-сервисе)

## Скрипты

### Генерация документации:

*открывает статические файлы в браузере*
```bash
dotnet pwsh ./scripts/generate-docs.ps1
```

### или 
*поднимает сервер для просмотра документации*
```bash
dotnet pwsh ./scripts/generate-docs.ps1 -Serve
```

### Генерация отчета о тестах:
```bash
dotnet pwsh ./scripts/generate-test-report.ps1
```

## Если Powershell не установился автоматически
Нужно выполнить команду
```bash
dotnet tool restore
```

## Компиляция и запуск кода в сервисе

Для запуска кода предварительно нужно установить образ докера для GCC и запуска.

### GCC
```bash
docker pull n0madic/alpine-gcc:14.3.0
```
### Контейнер для запуска

```bash
docker pull busybox:1.38.0-glibc
```

### Java
```bash
docker pull eclipse-temurin:21-jdk-alpine
```
### Контейнер для запуска

```bash
docker pull eclipse-temurin:21-jre-alpine
```

### C#
```bash
docker pull mcr.microsoft.com/dotnet/sdk:11.0-alpine
```
### Контейнер для запуска

```bash
docker pull mcr.microsoft.com/dotnet/runtime:11.0-alpine
```
