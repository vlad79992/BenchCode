# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/openjdk/jdk:25-ubuntu AS wasm-build
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates wget unzip libstdc++6 libatomic1 && \
    rm -rf /var/lib/apt/lists/*

COPY kotlin* ./
RUN chmod +x ./kotlin
COPY project.yaml libs.versions.toml ./
COPY shared/ ./shared/
COPY webApp/ ./webApp/

RUN sed -i '/^[[:space:]]*-[[:space:]]*androidApp[[:space:]]*$/d; /^[[:space:]]*-[[:space:]]*desktopApp[[:space:]]*$/d' project.yaml

ARG COMPOSE_WASM_VARIANT=release
RUN --mount=type=cache,target=/root/.cache \
    --mount=type=cache,target=/root/.kotlin \
    --mount=type=cache,target=/app/build \
    set -eu; \
    case "$COMPOSE_WASM_VARIANT" in \
      debug) task="_webApp_buildWasmJsAppWasmJsDebug" ;; \
      release) task="_webApp_buildWasmJsAppWasmJsRelease" ;; \
      *) echo "Unsupported COMPOSE_WASM_VARIANT: $COMPOSE_WASM_VARIANT" >&2; exit 1 ;; \
    esac; \
    ./kotlin build -m webApp -v "$COMPOSE_WASM_VARIANT"; \
    mkdir -p /app/export; \
    cp -a "build/tasks/$task/." /app/export/

FROM scratch AS frontend-export
COPY --from=wasm-build /app/export/ /
