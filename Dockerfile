# ==========================================
# Stage 1: Build Kotlin Compose Wasm App
# ==========================================
FROM mcr.microsoft.com/openjdk/jdk:25-ubuntu AS kotlin-build
WORKDIR /app

# Устанавливаем системные зависимости
RUN apt-get update && \
    apt-get install -y --no-install-recommends libatomic1 wget ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# 1. Копируем исполняемые скрипты Kotlin toolchain из папки ComposeWebApp
COPY ComposeWebApp/kotlin ./
COPY ComposeWebApp/kotlin.bat ./
RUN chmod +x ./kotlin

# 2. Копируем конфигурационные файлы Amper
COPY ComposeWebApp/project.yaml ./
COPY ComposeWebApp/libs.versions.toml ./

# 3. Копируем исходный код модулей приложения
COPY ComposeWebApp/shared/ ./shared/
COPY ComposeWebApp/webApp/ ./webApp/

RUN sed -i 's/- androidApp/# - androidApp/g' project.yaml && \
    sed -i 's/- desktopApp/# - desktopApp/g' project.yaml

# 4. Собираем Wasm production дистрибутив
# Временно замените финальную команду сборщика на эту, чтобы увидеть дерево папок в логах:

# RUN --mount=type=cache,target=/root/.cache \
#    --mount=type=cache,target=/root/.kotlin \
#     ./kotlin build -m webApp --variant release && \
#     echo "--- НАЧАЛО ПОЛНОГО СПИСКА ФАЙЛОВ ---" && \
#     find build/artifacts -type f && \
#     echo "--- КОНЕЦ ПОИСКА ФАЙЛОВ ---" && \
#     exit 1

RUN --mount=type=cache,target=/root/.cache \
    --mount=type=cache,target=/root/.kotlin \
    ./kotlin build -m webApp --variant release && \
    mkdir -p /app/ready_static && \
    # Копируем готовый дистрибутив webApp (index.html, *.mjs, *.wasm, vendors/, composeResources/),
    # сгенерированный Amper в build/tasks/_webApp_buildWasmJsAppWasmJsRelease
    TASK_DIR=$(find build/tasks -type d -name "_webApp_buildWasmJsAppWasmJsRelease" -o -name "_webApp_buildWasmJsAppWasmJs*" | head -n 1) && \
    cp -r "$TASK_DIR"/. /app/ready_static/

# ==========================================
# Stage 2: Build ASP.NET Core App
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BenchCodeMainService/BenchCodeMainService.csproj", "BenchCodeMainService/"]
RUN dotnet restore "BenchCodeMainService/BenchCodeMainService.csproj"
COPY . .
WORKDIR "/src/BenchCodeMainService"
RUN dotnet build "./BenchCodeMainService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BenchCodeMainService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ==========================================
# Stage 3: Final Image
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the compiled Wasm web app into the ASP.NET Core wwwroot folder
COPY --from=kotlin-build /app/ready_static ./wwwroot

ENTRYPOINT ["dotnet", "BenchCodeMainService.dll"]
