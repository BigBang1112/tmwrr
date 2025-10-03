# Na https://aka.ms/customizecontainer se dozvíte, jak si přizpůsobit kontejner ladění a jak Visual Studio používá tento dokument Dockerfile k sestavení vašich imagí pro rychlejší ladění.

# Tyto argumenty umožňují při ladění z VS vyměnit bázi použitou k vytvoření konečné image za jinou.
ARG LAUNCHING_FROM_VS
# Nastaví image báze na konečnou image, ale jenom v případě, že je definovaná možnost LAUNCHING_FROM_VS.
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# Tato fáze se používá při spuštění z VS v rychlém režimu (výchozí pro konfiguraci ladění).
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


# Tato fáze slouží k sestavení projektu služby.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Nainstalovat závislosti clang/zlib1g-dev pro publikování do nativního prostředí
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Templates/GenericHost/TMWRR.DiscordBot/TMWRR.DiscordBot.csproj", "Templates/GenericHost/TMWRR.DiscordBot/"]
RUN dotnet restore "./Templates/GenericHost/TMWRR.DiscordBot/TMWRR.DiscordBot.csproj"
COPY . .
WORKDIR "/src/Templates/GenericHost/TMWRR.DiscordBot"
RUN dotnet build "./TMWRR.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Tato fáze slouží k publikování projektu služby, který se má zkopírovat do konečné fáze.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TMWRR.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# Tato fáze se používá jako báze pro konečnou fázi při spouštění z VS pro podporu ladění v běžném režimu (výchozí, když se nepoužívá konfigurace ladění).
FROM base AS aotdebug
USER root
# Nainstalovat GDB pro podporu nativního ladění
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

# Tato fáze se používá v produkčním prostředí nebo při spuštění z VS v běžném režimu (výchozí, když se nepoužívá konfigurace ladění).
FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:9.0} AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./TMWRR.DiscordBot"]