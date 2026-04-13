FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Layer-cache optimised: copy project/props files first, restore, then full source
COPY backend/Directory.Build.props     backend/
COPY backend/Directory.Packages.props  backend/
COPY backend/services/Gateway/Gateway.csproj              backend/services/Gateway/
COPY backend/shared/Axon.Contracts/Axon.Contracts.csproj  backend/shared/Axon.Contracts/

RUN dotnet restore backend/services/Gateway/Gateway.csproj

COPY backend/services/Gateway/   backend/services/Gateway/
COPY backend/shared/Axon.Contracts/ backend/shared/Axon.Contracts/

WORKDIR /src/backend/services/Gateway
RUN dotnet publish Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN groupadd --system axon && useradd --system --gid axon axon
USER axon

ENTRYPOINT ["dotnet", "Gateway.dll"]


