FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/ThirdPartyBroadcast.Consumer ./ThirdPartyBroadcast.Consumer
COPY ./Sources/ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj ./ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj



WORKDIR /src/ThirdPartyBroadcast.Consumer.Service
RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN dotnet restore -v n

COPY ./Sources/ThirdPartyBroadcast.Consumer.Service .

RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app 

FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENV DOTNET_HOSTBUILDER_RELOADCONFIGONCHANGE = false
ENV DOTNET_USE_POLLING_FILE_WATCHER = true
ENTRYPOINT ["dotnet", "ThirdPartyBroadcast.Consumer.Service.dll"]
