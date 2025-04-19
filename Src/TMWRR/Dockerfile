FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETARCH
ARG APPNAME=TMWRR
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link Src/$APPNAME/*.csproj $APPNAME/
COPY --link Src/$APPNAME.DiscordReport/*.csproj $APPNAME.DiscordReport/
RUN dotnet restore -a $TARGETARCH $APPNAME/$APPNAME.csproj

# Copy source code and publish app
COPY --link Src/$APPNAME/ $APPNAME/
COPY --link Src/$APPNAME.DiscordReport/ $APPNAME.DiscordReport/

WORKDIR /src/$APPNAME
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
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
ENTRYPOINT ["./TMWRR"]