# Build context must be the repository root so the shared Contracts project is reachable.
# docker-compose sets context: . (repo root) and dockerfile: src/WorkItems.Worker/Dockerfile.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/WorkItems.Contracts/WorkItems.Contracts.csproj", "src/WorkItems.Contracts/"]
COPY ["src/WorkItems.Worker/WorkItems.Worker.csproj",         "src/WorkItems.Worker/"]
RUN dotnet restore "src/WorkItems.Worker/WorkItems.Worker.csproj"

COPY . .
RUN dotnet publish "src/WorkItems.Worker/WorkItems.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WorkItems.Worker.dll"]
