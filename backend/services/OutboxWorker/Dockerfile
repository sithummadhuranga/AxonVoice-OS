FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props     backend/
COPY backend/Directory.Packages.props  backend/
COPY backend/services/OutboxWorker/OutboxWorker.csproj    backend/services/OutboxWorker/
COPY backend/shared/Axon.Contracts/Axon.Contracts.csproj  backend/shared/Axon.Contracts/

RUN dotnet restore backend/services/OutboxWorker/OutboxWorker.csproj

COPY backend/services/OutboxWorker/  backend/services/OutboxWorker/
COPY backend/shared/Axon.Contracts/  backend/shared/Axon.Contracts/

WORKDIR /src/backend/services/OutboxWorker
RUN dotnet publish OutboxWorker.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN addgroup --system axon && adduser --system --ingroup axon axon
USER axon

ENTRYPOINT ["dotnet", "OutboxWorker.dll"]
