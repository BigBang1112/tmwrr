FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
ARG APPNAME=TMWRR
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link Src/$APPNAME/*.csproj $APPNAME/
COPY --link Src/$APPNAME.Api/*.csproj $APPNAME.Api/
COPY --link Src/$APPNAME.DiscordReport/*.csproj $APPNAME.DiscordReport/
RUN dotnet restore -a $TARGETARCH $APPNAME/$APPNAME.csproj

# Copy source code and publish app
COPY --link Src/$APPNAME/ $APPNAME/
COPY --link Src/$APPNAME.Api/ $APPNAME.Api/
COPY --link Src/$APPNAME.DiscordReport/ $APPNAME.DiscordReport/

WORKDIR /src/$APPNAME
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
EXPOSE 8080

RUN apk add --no-cache tzdata

WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./TMWRR"]