FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link Src/TMWR/*.csproj TMWR/
COPY --link Src/TMWR.DiscordReport/*.csproj TMWR.DiscordReport/
RUN dotnet restore -a $TARGETARCH TMWR/TMWR.csproj

# Copy source code and publish app
COPY --link Src/TMWR/ TMWR/
COPY --link Src/TMWR.DiscordReport/ TMWR.DiscordReport/

WORKDIR /src/TMWR
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./NationsConverterBot"]