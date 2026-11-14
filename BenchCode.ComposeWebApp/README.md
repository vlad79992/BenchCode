This is a Kotlin Multiplatform project targeting Android, Desktop (JVM), Web, built with the [Kotlin Toolchain](https://kotlin-toolchain.org/dev/).

- [/androidApp](./androidApp) contains the Android application.
- [/desktopApp](./desktopApp) contains the desktop (JVM) application.
- [/shared](./shared) holds the code shared across your applications — Compose UI, business logic, and platform-specific implementations. [src](./shared/src) is for common code; the sibling `src@<platform>` folders (for example `src@android`) hold code compiled only for the platform named in the folder.
- [/webApp](./webApp) contains the web application, compiled to WebAssembly with Kotlin/Wasm.

The `kotlin` (macOS/Linux) and `kotlin.bat` (Windows) scripts in the project root are self-bootstrapping wrappers for the Kotlin Toolchain: they download the pinned toolchain version on first use, so no separate installation is required. Build the whole project with `./kotlin build`.

### Running

Use the run configurations provided by the run widget in your IDE's toolbar. You can also run each module from the command line:

- Android app: `./kotlin run -m androidApp`
- Desktop app: `./kotlin run -m desktopApp`
- Web app: `./kotlin run -m webApp`

### Serving the WASM app with .NET Aspire

The Aspire AppHost uses a one-shot Linux container to build Compose WASM, exports the generated files into `BenchCode.Web/wwwroot`, then starts `BenchCode.Web` as a regular .NET process. Docker Engine is required for the frontend build; the ASP.NET Core host runs on the local .NET SDK. From the repository root, run the same command on Windows, Linux/WSL, or macOS:

```shell
dotnet run --project BenchCode.AppHost/BenchCode.AppHost.csproj
```

By default, the `debug` variant is used in the Development environment and the optimized `release` variant in other environments. To select a variant explicitly, pass `-- --Compose:Variant=release` (or `debug`) to `dotnet run`.

The Dockerfile and `.dockerignore` for the frontend build are in this directory, which the AppHost uses as the Docker build context. The AppHost configures the Docker build and selected variant in C#. BuildKit caches the Kotlin Toolchain, dependencies, and incremental build state between runs. The first frontend build takes longer while the Linux image, Kotlin Toolchain, and dependencies are provisioned. The web server itself is not built into or run from a container.

### Testing

Run the project's tests with `./kotlin test`, or `./kotlin test -m <module>` for a single module.

---

Learn more about [Kotlin Multiplatform](https://www.jetbrains.com/help/kotlin-multiplatform-dev/get-started.html), [Kotlin Toolchain](https://kotlin-toolchain.org/dev/), [Compose Multiplatform](https://kotlinlang.org/compose-multiplatform/), [Kotlin/Wasm](https://kotl.in/wasm/).
