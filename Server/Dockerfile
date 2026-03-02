# Build stage (Ubuntu 24.04 기반 런타임으로 배포)
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY ["PoodleJump.RankingApi.csproj", "."]
RUN dotnet restore "PoodleJump.RankingApi.csproj"

COPY . .
RUN dotnet build "PoodleJump.RankingApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PoodleJump.RankingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (Ubuntu 24.04 LTS 호환: bookworm-slim = Debian 12, .NET 8은 Ubuntu 24.04에서 동작)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PoodleJump.RankingApi.dll"]
