# Kotlin compose wasm

FROM eclipse-temurin:21-jdk AS kotlin-build
WORKDIR /app
RUN apt-get update && \
    apt-get install -y --no-install-recommends libatomic1 && \
    rm -rf /var/lib/apt/lists/*
COPY ComposeWebApp/ ./
RUN chmod +x ./gradlew
RUN ./gradlew :webApp:wasmJsBrowserDistribution --no-daemon

# asp.net core

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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=kotlin-build /app/webApp/build/dist/wasmJs/productionExecutable ./wwwroot/
ENTRYPOINT ["dotnet", "BenchCodeMainService.dll"]
