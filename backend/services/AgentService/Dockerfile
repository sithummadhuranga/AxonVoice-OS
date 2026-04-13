FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props     backend/
COPY backend/Directory.Packages.props  backend/
COPY backend/services/AgentService/AgentService.csproj    backend/services/AgentService/
COPY backend/shared/Axon.Contracts/Axon.Contracts.csproj  backend/shared/Axon.Contracts/

RUN dotnet restore backend/services/AgentService/AgentService.csproj

COPY backend/services/AgentService/  backend/services/AgentService/
COPY backend/shared/Axon.Contracts/  backend/shared/Axon.Contracts/

WORKDIR /src/backend/services/AgentService
RUN dotnet publish AgentService.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN addgroup --system axon && adduser --system --ingroup axon axon
USER axon

ENTRYPOINT ["dotnet", "AgentService.dll"]
