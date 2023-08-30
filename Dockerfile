FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

ARG BB_RUNNER_VERSION=0.4.10

WORKDIR /app

RUN apt-get update && \
    apt-get install --no-install-recommends -y \
    wget unzip && \
    rm -rf /var/lib/apt/lists/*

RUN wget -q https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner/releases/download/${BB_RUNNER_VERSION}/${BB_RUNNER_VERSION}.zip -O /tmp/runner.zip && \
    unzip -q /tmp/runner.zip -d /app && \
    rm /tmp/runner.zip

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app

ARG UNAME=bbr
ARG UID=1000
ARG GID=1000

RUN groupadd -g $GID -o $UNAME \
    && useradd -l -u $UID -g $GID -o -s /bin/bash $UNAME

WORKDIR /app
COPY --from=base --chown=$UID:$GID /app /app
COPY --chown=$UID:$GID docker/appsettings.json /app/appsettings.json
RUN mkdir -p data/modules data/dependencies data/configurations\
    && chown -R $UID:$GID /app

USER $UID:$GID

CMD ["dotnet", "BattleBitAPIRunner.dll"]