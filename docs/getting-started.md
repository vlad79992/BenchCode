# Начало работы

## Требования

Для разработки Kotlin-приложения понадобятся:

- Git для получения исходников.
- Java runtime для запуска инструментов и JVM-приложения. Репозиторные скрипты `kotlin` и `kotlin.bat` самостоятельно загружают закреплённый Kotlin Toolchain при первом запуске. Для воспроизводимой сборки Wasm через Dockerfile используется OpenJDK 25; workflow документации использует Temurin 21 для Dokka CLI.
- Доступ к сети при первом запуске: wrapper скачивает дистрибутив и зависимости Kotlin Toolchain. Для Android дополнительно нужны Android SDK и настроенные SDK-пакеты в IDE.
- Docker Engine, только если запускается полный Aspire-сценарий из корня репозитория.

Версия Kotlin CLI закреплена в `BenchCode.ComposeWebApp/kotlin` и `kotlin.bat` как **0.13.0**. Это версия Kotlin Toolchain CLI, а не номер версии языка Kotlin. Языковая версия в `project.yaml` явно не задана. Android SDK и Compose Multiplatform release также не имеют отдельных версий в `module.yaml`: Compose зависимости выбираются через `$compose.*` каталога Kotlin Toolchain.

## Получение репозитория

```shell
git clone https://github.com/vlad79992/BenchCode.git
cd BenchCode
```

Для Kotlin-команд перейдите в каталог проекта:

```shell
cd BenchCode.ComposeWebApp
```

## Сборка Kotlin-проекта

Windows:

```powershell
.\kotlin.bat build
```

Linux и macOS:

```shell
./kotlin build
```

Команда собирает модули, перечисленные в `project.yaml`: `androidApp`, `desktopApp`, `shared` и `webApp`. Kotlin Toolchain загружается автоматически, если его ещё нет в локальном кеше.

Для сборки одного модуля используйте `-m`:

```shell
./kotlin build -m shared
```

В Windows замените `./kotlin` на `.\kotlin.bat`.

## Локальный запуск

Запустить web-приложение через Kotlin Toolchain:

```shell
./kotlin run -m webApp
```

CLI выведет информацию о локальном запуске/сервере; откройте указанный URL в браузере. Для других таргетов:

```shell
./kotlin run -m desktopApp
./kotlin run -m androidApp
```

В Android потребуется эмулятор или подключённое устройство с настроенным SDK. Запуск из IDE через конфигурации проекта также поддерживается.

### Полный web-сценарий через Aspire

В проекте отдельно предусмотрена интеграция с .NET Aspire. Из корня репозитория выполните:

```shell
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj
```

AppHost собирает WASM-файлы Kotlin-приложения в одноразовом Linux-контейнере и экспортирует их в `BenchCode.Web/wwwroot`, после чего запускает ASP.NET Core host. Для этого сценария нужен запущенный Docker Engine и установленный .NET SDK. Это не требуется для отдельной команды `kotlin run -m webApp`.

## Тесты

Из каталога `BenchCode.ComposeWebApp` запустите все тесты:

```shell
./kotlin test
```

Или выберите модуль:

```shell
./kotlin test -m shared
```

Поддержка отдельных тестовых source sets видна в каталогах `shared/test`, `shared/test@android`, `shared/test@jvm` и `shared/test@wasmJs`.
