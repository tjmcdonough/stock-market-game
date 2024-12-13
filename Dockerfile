FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["StockMarketGame/StockMarketGame.csproj", "StockMarketGame/"]
RUN dotnet restore "./StockMarketGame/StockMarketGame.csproj"
COPY . .
WORKDIR "/src/StockMarketGame"
RUN dotnet build "./StockMarketGame.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./StockMarketGame.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StockMarketGame.dll"]
