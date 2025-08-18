# Na https://aka.ms/customizecontainer se dozvíte, jak si přizpůsobit kontejner ladění a jak Visual Studio používá tento dokument Dockerfile k sestavení vašich imagí pro rychlejší ladění.

# Tato fáze se používá při spuštění z VS v rychlém režimu (výchozí pro konfiguraci ladění).
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS base
USER $APP_UID
WORKDIR /app


# Tato fáze slouží k sestavení projektu služby.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/TMWRR.DiscordBot/TMWRR.DiscordBot.csproj", "Src/TMWRR.DiscordBot/"]
RUN dotnet restore "./Src/TMWRR.DiscordBot/TMWRR.DiscordBot.csproj"
COPY . .
WORKDIR "/src/Src/TMWRR.DiscordBot"
RUN dotnet build "./TMWRR.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Tato fáze slouží k publikování projektu služby, který se má zkopírovat do konečné fáze.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TMWRR.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Tato fáze se používá v produkčním prostředí nebo při spuštění z VS v běžném režimu (výchozí, když se nepoužívá konfigurace ladění).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TMWRR.DiscordBot.dll"]