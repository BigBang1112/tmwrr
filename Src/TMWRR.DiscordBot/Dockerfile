FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
ARG APPNAME=TMWRR
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link Src/$APPNAME.DiscordBot/*.csproj $APPNAME.DiscordBot/
COPY --link Src/$APPNAME.Api/*.csproj $APPNAME.Api/
RUN dotnet restore -a $TARGETARCH $APPNAME.DiscordBot/$APPNAME.DiscordBot.csproj

# Copy source code and publish app
COPY --link Src/$APPNAME.DiscordBot/ $APPNAME.DiscordBot/
COPY --link Src/$APPNAME.Api/ $APPNAME.Api/

WORKDIR /src/$APPNAME.DiscordBot
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
EXPOSE 8080

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs

WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./TMWRR.DiscordBot"]