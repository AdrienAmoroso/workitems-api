# Build context must be the repository root so the shared Contracts project is reachable.
# docker-compose sets context: . (repo root) and dockerfile: src/WorkItems.Api/Dockerfile.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so layer-cached restore is only invalidated when deps change.
COPY ["src/WorkItems.Contracts/WorkItems.Contracts.csproj", "src/WorkItems.Contracts/"]
COPY ["src/WorkItems.Api/WorkItems.Api.csproj",             "src/WorkItems.Api/"]
RUN dotnet restore "src/WorkItems.Api/WorkItems.Api.csproj"

COPY . .
RUN dotnet publish "src/WorkItems.Api/WorkItems.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Required by Npgsql (PostgreSQL .NET driver) on Debian-based images.
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "WorkItems.Api.dll"]
