Это репозиторий проекта CodeBench, реализуемого в рамках предмета "Технологии разработки ПО"

# Запуск
Для сборки _всего_ проекта использовать команду (очень долго из-за градла)
```
docker compose up --build
```

## Запуск фронтенда
Все команды выполнять в папке [/ComposeWebApp](./ComposeWebApp)
### JVM 
Самый быстрый вариант
```
./gradlew :desktopApp:run
```
### WASM
Медленнее, но запускается в браузере
```
./gradlew :webApp:wasmJsBrowserDevelopmentRun
```

# Важные ресурсы:

[Material 3 expressive](
https://m3.material.io/blog/building-with-m3-expressive) показывается, как должны выглядеть элементы интерфейса, есть детальное описание почти каждого элемента

[Документация kotlin multiplatform](https://kotlinlang.org/docs/multiplatform/get-started.html)

[Документация kotlin multiplatform (android developers)](https://developer.android.com/kotlin/multiplatform?hl=ru) многое работает везде

[Kotlin toolchain](https://kotlin-toolchain.org/dev/) (система сборки)

[Документация C#](https://learn.microsoft.com/ru-ru/dotnet/csharp/)

[Дорожная карта для разработчиков Java, изучающих C#](https://learn.microsoft.com/ru-ru/dotnet/csharp/tour-of-csharp/tips-for-java-developers)
