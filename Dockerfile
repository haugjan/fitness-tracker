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

COPY FitnessTracker.Contracts/ FitnessTracker.Contracts/
COPY FitnessTracker.Client/ FitnessTracker.Client/
COPY FitnessTracker/ FitnessTracker/

# Der Host referenziert den Client, darum landet das WebAssembly-Bundle
# mit diesem einen Publish bereits unter wwwroot.
RUN dotnet publish FitnessTracker/FitnessTracker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FitnessTracker.dll"]
