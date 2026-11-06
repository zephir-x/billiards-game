# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files first for optimized layer caching
COPY ["Interfaces/BilliardsGame.Interfaces.csproj", "Interfaces/"]
COPY ["Network/BilliardsGame.Network.csproj", "Network/"]
COPY ["Server/BilliardsGame.RelayServer/BilliardsGame.RelayServer.csproj", "Server/BilliardsGame.RelayServer/"]

RUN dotnet restore "Server/BilliardsGame.RelayServer/BilliardsGame.RelayServer.csproj"

# Copy source code and publish
COPY Interfaces/ Interfaces/
COPY Network/ Network/
COPY Server/ Server/

WORKDIR "/src/Server/BilliardsGame.RelayServer"
RUN dotnet publish "BilliardsGame.RelayServer.csproj" -c Release -o /app/publish \
    --no-restore \
    -p:UseAppHost=false

# Runtime Stage: Ultra-lightweight Alpine container (~50MB total)
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
WORKDIR /app

ENV RELAY_PORT=7777 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 7777

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BilliardsGame.RelayServer.dll"]
