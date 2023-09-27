FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0-alpine AS base
WORKDIR /app

FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src
COPY ["Sources/ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj", "ThirdPartyBroadcast.Consumer.Service/"]
COPY ["Sources/ThirdPartyBroadcast.Consumer/ThirdPartyBroadcast.Consumer.csproj", "ThirdPartyBroadcast.Consumer/"]

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN dotnet restore "ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj"

COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app 

FROM base AS final
WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["dotnet", "ThirdPartyBroadcast.Consumer.Service.dll"]
