# BenchCode

**A cross-platform competition and task management application built with Kotlin Multiplatform, Compose Multiplatform, and .NET Aspire.**

[🇬🇧 English](#english) | [🇷🇺 Русский](#русский)

---

# English

## Overview

**BenchCode** is a cross-platform application project designed around competitions, participants, and tasks.

The repository combines a Kotlin Multiplatform client with a .NET-based backend infrastructure. The client uses Compose Multiplatform to share UI and application logic across Android, Desktop, and Web (WebAssembly).

The backend infrastructure uses .NET Aspire to orchestrate application services, PostgreSQL for persistent storage, and OpenIddict with ASP.NET Core Identity for authentication.

## Features

### Cross-platform client

- Shared UI and application logic using Kotlin Multiplatform.
- Android application.
- Desktop application (JVM).
- Web application compiled to WebAssembly.
- Compose Multiplatform and Material 3 UI.
- Navigation 3 for application navigation.
- Koin for dependency injection.
- Kotlin Coroutines and Flow for asynchronous operations.
- DataStore Preferences for local settings persistence.

### Backend infrastructure

- .NET Aspire application orchestration.
- ASP.NET Core web hosting.
- PostgreSQL database integration.
- Entity Framework Core.
- ASP.NET Core Identity user management.
- OpenIddict authentication and token management.
- OAuth 2.0 password and refresh token grant support.
- JWT-based access tokens.
- API endpoints for user registration and profile retrieval.

## Technology Stack

| Technology | Purpose |
|---|---|
| Kotlin | Main client programming language |
| Kotlin Multiplatform | Cross-platform application logic |
| Compose Multiplatform | Shared user interface |
| Kotlin/Wasm | Web application compilation |
| Material 3 | UI components and design |
| Navigation 3 | Application navigation |
| Koin | Dependency injection |
| Coroutines / Flow | Asynchronous operations |
| DataStore | Local preferences storage |
| .NET / ASP.NET Core | Backend infrastructure |
| .NET Aspire | Service orchestration |
| PostgreSQL | Database |
| Entity Framework Core | Database access |
| ASP.NET Core Identity | User management |
| OpenIddict | Authentication and authorization |

## Architecture

The repository consists of several independent application modules and infrastructure projects.

```text
BenchCode/
│
├── BenchCode.AppHost/
│   └── Aspire application orchestration
│
├── BenchCode.AuthService/
│   ├── Controllers/
│   │   ├── AuthorizationController.cs
│   │   └── UsersController.cs
│   ├── Entities/
│   └── Authentication and user management
│
├── BenchCode.ServiceDefaults/
│   └── Shared service configuration
│
├── BenchCode.Web/
│   └── ASP.NET Core static web hosting
│
├── BenchCode.ComposeWebApp/
│   ├── androidApp/
│   ├── desktopApp/
│   ├── shared/
│   ├── webApp/
│   ├── Dockerfile
│   └── Kotlin Toolchain configuration
│
├── BenchCode.Test/
│   └── Automated tests
│
├── docs/
│   └── Project documentation
│
└── BenchCode.slnx
```

### Client architecture

The `BenchCode.ComposeWebApp` project contains four Kotlin modules:

| Module | Description |
|---|---|
| `shared` | Shared UI, navigation, ViewModels, repositories, and business logic |
| `androidApp` | Android application entry point |
| `desktopApp` | Desktop JVM application entry point |
| `webApp` | WebAssembly application entry point |

The shared module targets Android, JVM, and Wasm JS.

Platform-specific implementations are separated into dedicated source sets, while common application logic remains in the shared source set.

### Backend architecture

The .NET infrastructure consists of:

- **AppHost** — coordinates application services and frontend build.
- **AuthService** — handles user registration, authentication, and token issuance.
- **Web** — serves the compiled WebAssembly frontend.
- **ServiceDefaults** — provides shared service configuration.
- **PostgreSQL** — stores authentication-related data.

The AppHost manages the PostgreSQL resource, AuthService, and Web application.

### Authentication

The authentication service uses OpenIddict and ASP.NET Core Identity.

Implemented endpoints:

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/users/register` | Register a new user |
| POST | `/connect/token` | Obtain authentication tokens |
| GET | `/api/users/me` | Retrieve the authenticated user's profile |

Supported token grant types:

- Password grant.
- Refresh token grant.

Access tokens use the `benchcode-api` audience.

The Compose client is registered as a public OAuth client.

## Getting Started

### Requirements

- Git
- Java runtime compatible with the Kotlin Toolchain
- .NET SDK 11.0
- Docker Engine with Buildx
- Android SDK (for Android development)
- An IDE with Kotlin Multiplatform support (recommended)

Docker is required for the full Aspire workflow, but not for running the Kotlin Web application independently.

### Clone the repository

```bash
git clone https://github.com/vlad79992/BenchCode.git
cd BenchCode
```

### Run the Kotlin application

Navigate to the Kotlin project:

```bash
cd BenchCode.ComposeWebApp
```

The repository includes self-bootstrapping Kotlin Toolchain wrappers.

Build the project:

**Linux / macOS**

```bash
./kotlin build
```

**Windows**

```powershell
.\kotlin.bat build
```

The wrapper automatically downloads the pinned Kotlin Toolchain when needed.

### Run individual applications

**Android**

```bash
./kotlin run -m androidApp
```

**Desktop**

```bash
./kotlin run -m desktopApp
```

**Web**

```bash
./kotlin run -m webApp
```

The Web application can run independently without the .NET Aspire infrastructure.

### Run the complete Aspire environment

Return to the repository root and execute:

```bash
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj
```

Aspire will:

1. Build the Kotlin WebAssembly frontend using Docker Buildx.
2. Export the generated frontend files into `BenchCode.Web/wwwroot`.
3. Start the PostgreSQL database.
4. Start the ASP.NET Core Web application.
5. Start the authentication service.

The frontend build uses the `debug` variant in Development and `release` in other environments by default.

To explicitly select a variant:

```bash
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj -- --Compose:Variant=release
```

Available variants: `debug`, `release`.

## Testing

Run all Kotlin tests:

```bash
./kotlin test
```

Run tests for a specific module:

```bash
./kotlin test -m shared
```

The repository also contains a .NET test project:

```bash
dotnet test BenchCode.Test/BenchCode.Test.csproj
```

## Documentation

Additional technical documentation is available in the `docs/` directory:

- Architecture
- Getting Started
- Development Guide
- Technologies
- Troubleshooting
- Documentation Guide

## Current Development Status

BenchCode is under active development.

The current client includes the initial navigation structure, role selection, student competition selection, a competition screen, and settings.

The authentication service implements user registration, token issuance, refresh tokens, and authenticated profile retrieval.

Some application workflows are still incomplete. In particular, the teacher role does not yet have an implemented action, and the competition screen currently displays the supplied competition identifier rather than retrieving competition data from a backend API.

The repository should therefore be considered a work in progress rather than a finished production application.

## Security Notes

- Do not commit production credentials or certificates.
- Configure signing and encryption certificates outside Development.
- Store secrets in environment variables or a dedicated secret manager.
- Use HTTPS for authentication endpoints.
- Review authentication grant types and deployment configuration before production use.

## License

No repository-level license has been specified. Please contact the repository owner regarding usage and distribution permissions.

---

# Русский

## О проекте

**BenchCode** — кроссплатформенный проект приложения для работы с соревнованиями, участниками и заданиями.

Репозиторий объединяет клиентское приложение на Kotlin Multiplatform и серверную инфраструктуру на .NET.

Клиентская часть использует Compose Multiplatform, что позволяет использовать общую логику и интерфейс на Android, Desktop и Web (WebAssembly).

Серверная инфраструктура построена на .NET Aspire и включает PostgreSQL для хранения данных, а также OpenIddict и ASP.NET Core Identity для аутентификации и управления пользователями.

## Возможности

### Кроссплатформенное приложение

- Общая бизнес-логика и UI на Kotlin Multiplatform.
- Приложение для Android.
- Desktop-приложение (JVM).
- Web-приложение, компилируемое в WebAssembly.
- Compose Multiplatform и Material 3.
- Navigation 3 для навигации.
- Koin для внедрения зависимостей.
- Kotlin Coroutines и Flow для асинхронных операций.
- DataStore Preferences для локального хранения настроек.

### Серверная инфраструктура

- Оркестрация сервисов через .NET Aspire.
- ASP.NET Core.
- Интеграция с PostgreSQL.
- Entity Framework Core.
- ASP.NET Core Identity для управления пользователями.
- OpenIddict для аутентификации и управления токенами.
- OAuth 2.0: Password Grant и Refresh Token Grant.
- JWT-токены доступа.
- API для регистрации пользователей и получения профиля.

## Технологический стек

| Технология | Назначение |
|---|---|
| Kotlin | Основной язык клиентской части |
| Kotlin Multiplatform | Кроссплатформенная логика |
| Compose Multiplatform | Общий пользовательский интерфейс |
| Kotlin/Wasm | Компиляция Web-приложения |
| Material 3 | UI-компоненты и дизайн |
| Navigation 3 | Навигация приложения |
| Koin | Внедрение зависимостей |
| Coroutines / Flow | Асинхронные операции |
| DataStore | Локальное хранение настроек |
| .NET / ASP.NET Core | Серверная инфраструктура |
| .NET Aspire | Оркестрация сервисов |
| PostgreSQL | База данных |
| Entity Framework Core | Работа с базой данных |
| ASP.NET Core Identity | Управление пользователями |
| OpenIddict | Аутентификация и авторизация |

## Архитектура

Репозиторий состоит из нескольких независимых модулей приложения и инфраструктурных проектов.

```text
BenchCode/
│
├── BenchCode.AppHost/
│   └── Оркестрация приложения через Aspire
│
├── BenchCode.AuthService/
│   ├── Controllers/
│   │   ├── AuthorizationController.cs
│   │   └── UsersController.cs
│   ├── Entities/
│   └── Аутентификация и управление пользователями
│
├── BenchCode.ServiceDefaults/
│   └── Общая конфигурация сервисов
│
├── BenchCode.Web/
│   └── Раздача статического Web-клиента
│
├── BenchCode.ComposeWebApp/
│   ├── androidApp/
│   ├── desktopApp/
│   ├── shared/
│   ├── webApp/
│   ├── Dockerfile
│   └── Конфигурация Kotlin Toolchain
│
├── BenchCode.Test/
│   └── Автоматизированные тесты
│
├── docs/
│   └── Документация проекта
│
└── BenchCode.slnx
```

### Архитектура клиентской части

Проект `BenchCode.ComposeWebApp` содержит четыре Kotlin-модуля:

| Модуль | Описание |
|---|---|
| `shared` | Общий UI, навигация, ViewModel, репозитории и бизнес-логика |
| `androidApp` | Точка входа Android-приложения |
| `desktopApp` | Точка входа Desktop-приложения |
| `webApp` | Точка входа WebAssembly-приложения |

Модуль `shared` поддерживает Android, JVM и Wasm JS.

Платформозависимые реализации располагаются в отдельных source sets, а общая логика приложения находится в shared-коде.

### Архитектура серверной части

.NET-инфраструктура включает:

- **AppHost** — управляет сервисами и сборкой фронтенда.
- **AuthService** — отвечает за регистрацию, аутентификацию и выдачу токенов.
- **Web** — обслуживает скомпилированный WebAssembly-клиент.
- **ServiceDefaults** — содержит общую конфигурацию сервисов.
- **PostgreSQL** — хранит данные, связанные с аутентификацией.

AppHost управляет ресурсом PostgreSQL, AuthService и Web-приложением.

### Аутентификация

Для аутентификации используются OpenIddict и ASP.NET Core Identity.

Реализованные эндпоинты:

| Метод | Эндпоинт | Описание |
|---|---|---|
| POST | `/api/users/register` | Регистрация нового пользователя |
| POST | `/connect/token` | Получение токенов аутентификации |
| GET | `/api/users/me` | Получение профиля авторизованного пользователя |

Поддерживаемые типы выдачи токенов:

- Password Grant.
- Refresh Token Grant.

Токены доступа используют аудиторию `benchcode-api`.

Compose-клиент зарегистрирован как публичный OAuth-клиент.

## Начало работы

### Требования

- Git
- Java Runtime, совместимый с Kotlin Toolchain
- .NET SDK 11.0
- Docker Engine с поддержкой Buildx
- Android SDK (для разработки под Android)
- IDE с поддержкой Kotlin Multiplatform (рекомендуется)

Docker необходим для полного запуска через Aspire, но не требуется для самостоятельного запуска Kotlin Web-приложения.

### Клонирование репозитория

```bash
git clone https://github.com/vlad79992/BenchCode.git
cd BenchCode
```

### Запуск Kotlin-приложения

Перейдите в каталог Kotlin-проекта:

```bash
cd BenchCode.ComposeWebApp
```

В репозитории предусмотрены скрипты автоматической загрузки Kotlin Toolchain.

Сборка проекта:

**Linux / macOS**

```bash
./kotlin build
```

**Windows**

```powershell
.\kotlin.bat build
```

При необходимости скрипт автоматически скачает закреплённую версию Kotlin Toolchain.

### Запуск отдельных приложений

**Android**

```bash
./kotlin run -m androidApp
```

**Desktop**

```bash
./kotlin run -m desktopApp
```

**Web**

```bash
./kotlin run -m webApp
```

Web-приложение можно запускать независимо от .NET Aspire.

### Запуск всей инфраструктуры Aspire

Вернитесь в корневой каталог репозитория и выполните:

```bash
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj
```

Aspire выполнит следующие действия:

1. Соберёт Kotlin WebAssembly-клиент с помощью Docker Buildx.
2. Экспортирует готовые файлы в `BenchCode.Web/wwwroot`.
3. Запустит базу данных PostgreSQL.
4. Запустит ASP.NET Core Web-приложение.
5. Запустит сервис аутентификации.

По умолчанию используется вариант `debug` в Development и `release` в остальных окружениях.

Чтобы явно выбрать вариант сборки:

```bash
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj -- --Compose:Variant=release
```

Доступные варианты: `debug`, `release`.

## Тестирование

Запуск всех Kotlin-тестов:

```bash
./kotlin test
```

Запуск тестов отдельного модуля:

```bash
./kotlin test -m shared
```

В репозитории также присутствует .NET-проект тестирования:

```bash
dotnet test BenchCode.Test/BenchCode.Test.csproj
```

## Документация

Дополнительная техническая документация находится в каталоге `docs/`:

- Архитектура
- Начало работы
- Руководство разработчика
- Технологии
- Решение проблем
- Руководство по документации

## Текущее состояние разработки

BenchCode находится в активной разработке.

На текущем этапе клиентская часть содержит начальную структуру навигации, выбор роли, выбор соревнования для студента, экран соревнования и настройки.

Сервис аутентификации реализует регистрацию пользователей, выдачу токенов, обновление токенов и получение профиля авторизованного пользователя.

Некоторые сценарии приложения пока не завершены. В частности, действие для роли преподавателя ещё не реализовано, а экран соревнования пока отображает переданный идентификатор вместо получения данных соревнования через серверный API.

Поэтому проект следует рассматривать как находящийся в разработке, а не как готовое production-решение.

## Безопасность

- Не добавляйте production-учётные данные и сертификаты в репозиторий.
- Настраивайте сертификаты подписи и шифрования вне Development.
- Храните секреты в переменных окружения или специализированных хранилищах.
- Используйте HTTPS для эндпоинтов аутентификации.
- Перед production-развёртыванием проверьте используемые типы выдачи токенов и конфигурацию безопасности.

## Лицензия

На уровне корневого репозитория отдельная лицензия не указана. Для получения разрешения на использование и распространение проекта обратитесь к владельцу репозитория.

---

**Repository:** https://github.com/vlad79992/BenchCode

**Branch:** `aspire`
