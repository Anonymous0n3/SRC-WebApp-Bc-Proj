# 0. FÁZE STAŽENÍ MODELU (Stáhne a rozbalí Vosk model)
FROM debian:bookworm-slim AS model-downloader
RUN apt-get update && apt-get install -y wget unzip
WORKDIR /download
# Stažení 1.8 GB modelu a jeho tiché rozbalení (parametr -q znamená quiet)
RUN wget -q https://alphacephei.com/vosk/models/vosk-model-en-us-0.22.zip && \
    unzip -q vosk-model-en-us-0.22.zip && \
    rm vosk-model-en-us-0.22.zip

# 1. RUNTIME FÁZE
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root

# Instalace FFmpeg a nezbytných knihoven pro nativní C++ kód (Vosk)
RUN apt-get update && \
    apt-get install -y ffmpeg libatomic1 libc6 libgcc-s1 libstdc++6 && \
    rm -rf /var/lib/apt/lists/*

# Nastavení cesty pro vyhledávání knihoven
ENV LD_LIBRARY_PATH="/app:${LD_LIBRARY_PATH}"

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. SDK FÁZE (Build frontendu i backendu)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update && apt-get install -y curl && \
    curl -sL https://deb.nodesource.com/setup_20.x | bash && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SRC-WebApp-Bc-Proj.Server/SRC-WebApp-Bc-Proj.Server.csproj", "SRC-WebApp-Bc-Proj.Server/"]
COPY ["src-webapp-bc-proj.client/src-webapp-bc-proj.client.esproj", "src-webapp-bc-proj.client/"]
RUN dotnet restore "./SRC-WebApp-Bc-Proj.Server/SRC-WebApp-Bc-Proj.Server.csproj"
COPY . .
WORKDIR "/src/SRC-WebApp-Bc-Proj.Server"
RUN dotnet build "./SRC-WebApp-Bc-Proj.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. PUBLISH FÁZE
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SRC-WebApp-Bc-Proj.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. FINÁLNÍ PRODUKÈNÍ IMAGE
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# TADY JE ZMÌNA: Kopírování modelu z naší nové downloader fáze
# Vytvoøíme složku models a zkopírujeme do ní stažený model
RUN mkdir -p ./models
COPY --from=model-downloader /download/vosk-model-en-us-0.22 ./models/vosk-model-en-us-0.22

# 1. Najde libvosk.so kdekoli v publikovaných souborech
# 2. Zkopíruje ho pøímo do /app, aby byl vedle SRC-WebApp-Bc-Proj.Server.dll
RUN find . -name "libvosk.so" -exec cp {} /app/libvosk.so \; && \
    chmod +x /app/libvosk.so

ENTRYPOINT ["dotnet", "SRC-WebApp-Bc-Proj.Server.dll"]