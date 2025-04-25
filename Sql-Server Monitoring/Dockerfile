FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Install Node.js
RUN apt-get update && \
    apt-get install -y wget && \
    apt-get install -y gnupg2 && \
    mkdir -p /etc/apt/keyrings && \
    wget -O - https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs

WORKDIR /src
COPY ["Sql-Server Monitoring/Sql-Server Monitoring.csproj", "Sql-Server Monitoring/"]
RUN dotnet restore "Sql-Server Monitoring/Sql-Server Monitoring.csproj"
COPY . .
WORKDIR "/src/Sql-Server Monitoring"
RUN npm ci --prefix ClientApp
RUN npm run build --prefix ClientApp
RUN dotnet build "Sql-Server Monitoring.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sql-Server Monitoring.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sql-Server Monitoring.dll"] 