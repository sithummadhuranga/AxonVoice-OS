FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props     backend/
COPY backend/Directory.Packages.props  backend/
COPY backend/services/Orchestrator/Orchestrator.csproj    backend/services/Orchestrator/
COPY backend/shared/Axon.Contracts/Axon.Contracts.csproj  backend/shared/Axon.Contracts/

RUN dotnet restore backend/services/Orchestrator/Orchestrator.csproj

COPY backend/services/Orchestrator/  backend/services/Orchestrator/
COPY backend/shared/Axon.Contracts/  backend/shared/Axon.Contracts/

WORKDIR /src/backend/services/Orchestrator
RUN dotnet publish Orchestrator.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Audio filler files mounted at runtime via volume — do not bake into image
RUN mkdir -p /app/audio

RUN addgroup --system axon && adduser --system --ingroup axon axon
USER axon

ENTRYPOINT ["dotnet", "Orchestrator.dll"]
