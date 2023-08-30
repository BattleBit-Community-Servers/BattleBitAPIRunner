FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

ARG BB_RUNNER_VERSION=0.4.9

WORKDIR /app

RUN apt-get update && \
    apt-get install --no-install-recommends -y \
    wget unzip && \
    rm -rf /var/lib/apt/lists/*

RUN wget -q https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner/releases/download/${BB_RUNNER_VERSION}/${BB_RUNNER_VERSION}.zip -O /tmp/runner.zip && \
    unzip -q /tmp/runner.zip -d /app && \
    rm /tmp/runner.zip

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app

WORKDIR /app
COPY --from=base /app /app
COPY docker/appsettings.json /app/appsettings.json

CMD ["dotnet", "BattleBitAPIRunner.dll"]