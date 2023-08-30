# Base image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base

WORKDIR /app
COPY . .
RUN dotnet publish -c Release --output ./bld/ BattleBitAPIRunner.sln

# App image
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app

ARG UNAME=bbr
ARG UID=1000
ARG GID=1000

RUN groupadd -g $GID -o $UNAME \
    && useradd -l -u $UID -g $GID -o -s /bin/bash $UNAME

WORKDIR /app
COPY --from=base --chown=$UID:$GID /app/bld /app
COPY --chown=$UID:$GID docker/appsettings.json /app/appsettings.json
RUN mkdir -p data/modules data/dependencies data/configurations\
    && chown -R $UID:$GID /app

USER $UID:$GID

VOLUME ["/app/data"]

CMD ["dotnet", "BattleBitAPIRunner.dll"]