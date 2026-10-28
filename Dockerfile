FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props .
COPY FitnessTracker.Contracts/FitnessTracker.Contracts.csproj FitnessTracker.Contracts/
COPY FitnessTracker.Client/FitnessTracker.Client.csproj FitnessTracker.Client/
COPY FitnessTracker/FitnessTracker.csproj FitnessTracker/
RUN dotnet restore FitnessTracker/FitnessTracker.csproj
RUN dotnet restore FitnessTracker.Client/FitnessTracker.Client.csproj

COPY FitnessTracker.Contracts/ FitnessTracker.Contracts/
COPY FitnessTracker.Client/ FitnessTracker.Client/
COPY FitnessTracker/ FitnessTracker/

RUN dotnet publish FitnessTracker/FitnessTracker.csproj -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish FitnessTracker.Client/FitnessTracker.Client.csproj -c Release -o /app/client

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Nur der eigenständige Publish des Clients ersetzt die Fingerprint-Platzhalter in
# index.html. Über den Projektverweis des Hosts bleibt dort "blazor.webassembly#[...]"
# stehen, und der Browser lädt dann nichts. Darum überschreibt das Client-Ergebnis
# das wwwroot aus dem Host-Publish.
COPY --from=build /app/client/wwwroot wwwroot/

ENTRYPOINT ["dotnet", "FitnessTracker.dll"]
