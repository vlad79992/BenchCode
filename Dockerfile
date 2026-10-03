# ==========================================
# Stage 1: Build Kotlin Compose Wasm App
# ==========================================
FROM eclipse-temurin:21-jdk AS kotlin-build
WORKDIR /app

# Install system dependencies often required by Kotlin tooling/Skiko
RUN apt-get update && \
    apt-get install -y --no-install-recommends libatomic1 && \
    rm -rf /var/lib/apt/lists/*

# Copy Gradle wrapper
COPY ComposeWebApp/gradlew ./
COPY ComposeWebApp/gradle/ ./gradle/
RUN chmod +x ./gradlew

# --- DEPENDENCY CACHING STRATEGY ---
# 1. Copy root and subproject build files FIRST
COPY ComposeWebApp/settings.gradle.kts ComposeWebApp/build.gradle.kts ComposeWebApp/gradle.properties ./
# If you use a version catalog (libs.versions.toml), copy it here too:
COPY ComposeWebApp/gradle/libs.versions.toml ./gradle/

# 2. Copy the build files of your subprojects so Gradle can resolve their dependencies
COPY ComposeWebApp/webApp/build.gradle.kts ./webApp/
COPY ComposeWebApp/shared/build.gradle.kts ./shared/

# 3. Download all dependencies (this layer is now cached unless build files change!)
RUN --mount=type=cache,target=/root/.gradle \
    --mount=type=cache,target=/root/.konan \
    --mount=type=cache,target=/root/.cache \
    --mount=type=cache,target=/root/.kotlin \
    ./gradlew :webApp:dependencies :shared:dependencies --no-daemon || true

# 4. Copy the rest of the source code
COPY ComposeWebApp/ ./

RUN apt-get update && apt-get install -y dos2unix && \
    dos2unix ./gradlew && \
    chmod +x ./gradlew && \
    rm -rf /var/lib/apt/lists/*

# 5. Build the Wasm production distribution
RUN --mount=type=cache,target=/root/.gradle \
    --mount=type=cache,target=/root/.konan \
    --mount=type=cache,target=/root/.cache \
    --mount=type=cache,target=/root/.kotlin \
    ./gradlew :webApp:wasmJsBrowserDistribution --no-daemon --build-cache

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
COPY --from=kotlin-build /app/webApp/build/dist/wasmJs/productionExecutable ./wwwroot/
ENTRYPOINT ["dotnet", "BenchCodeMainService.dll"]
