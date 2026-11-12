# Используемые технологии

## Kotlin и Kotlin Toolchain / Amper

Код приложения написан на Kotlin Multiplatform и собирается Kotlin Toolchain. В репозитории используется декларативная конфигурация Amper (`project.yaml` и файлы `module.yaml`), а не Gradle build scripts. Скрипты `kotlin` и `kotlin.bat` закрепляют Kotlin Toolchain CLI версии 0.13.0 и автоматически загружают его с проверкой SHA-256.

Amper позволяет описывать состав проекта и зависимости рядом с модулями, а общие исходники и платформенные source sets собирать в одном проекте. Сборка и запуск выполняются командами `kotlin build`, `kotlin run` и `kotlin test`.

- [Kotlin Toolchain](https://kotlin-toolchain.org/dev/)
- [Amper](https://amper.org/)
- [Kotlin Multiplatform](https://www.jetbrains.com/help/kotlin-multiplatform-dev/get-started.html)

## Kotlin Multiplatform и Compose Multiplatform

Модуль `shared` объявляет платформы `jvm`, `android` и `wasmJs`. Compose Multiplatform используется для общего UI на Android, Desktop и Web/Wasm; platform-specific source sets оставляют различия в точках входа, DataStore и небольших `expect`/`actual` API.

Версии Compose зависимостей подключаются через встроенные алиасы Kotlin Toolchain (`$compose.*`), а не перечислены как отдельные номера в `libs.versions.toml`. Языковая версия Kotlin также явно не зафиксирована в `project.yaml`; при обновлении Toolchain проверьте совместимость всех трёх targets.

- [Compose Multiplatform](https://kotlinlang.org/compose-multiplatform/)
- [Kotlin/Wasm](https://kotlinlang.org/docs/wasm-overview.html)
- [Kotlin Multiplatform source sets](https://www.jetbrains.com/help/kotlin-multiplatform-dev/multiplatform-discover-project.html)

## UI, состояние и маршрутизация

- **Compose UI и Material 3** задают composable-экраны, layout и визуальные компоненты.
- **AndroidX Lifecycle ViewModel** хранит состояние экранов вне composable-функций; в текущем коде Settings и студенческие экраны используют ViewModel.
- **Navigation 3** хранит сериализуемые маршруты в back stack; Koin Navigation 3 связывает маршруты с composable destinations.
- **Koin** регистрирует общие и платформенные зависимости и предоставляет ViewModel.
- **Kotlin Coroutines / Flow** используются для асинхронных операций и потока значений DataStore.
- **DataStore Preferences** хранит небольшие пользовательские настройки; реализация и место хранения зависят от target.

Версии библиотек указаны в [`libs.versions.toml`](https://github.com/vlad79992/BenchCode/blob/main/BenchCode.ComposeWebApp/libs.versions.toml), а использование — в `shared/module.yaml` и platform-specific source sets. Набор зависимостей и состояние отдельных библиотек могут меняться, поэтому при обновлении проверяйте конфигурацию проекта, а не только этот обзор.

- [Koin](https://insert-koin.io/)
- [AndroidX DataStore](https://developer.android.com/topic/libraries/architecture/datastore)
- [Kotlin Coroutines](https://kotlinlang.org/docs/coroutines-overview.html)
- [Navigation 3](https://developer.android.com/guide/navigation/navigation-3)
- [Compose Multiplatform navigation](https://www.jetbrains.com/compose-multiplatform/navigation/)

## Документация

DocFX собирает Markdown-страницы и TOC в единый сайт. Dokka CLI анализирует Kotlin-исходники и создаёт HTML API reference, который DocFX копирует в раздел `kotlin-api/`. CLI используется потому, что приложение собирается через Amper, а не Gradle.

- [DocFX](https://dotnet.github.io/docfx/)
- [Dokka CLI](https://kotlinlang.org/docs/dokka-cli.html)
