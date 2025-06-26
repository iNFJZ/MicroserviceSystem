# Multi-stage build for Microservice System
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["MicroserviceSystem.sln", "./"]
COPY GatewayApi/ocelot.json /app/GatewayApi/ocelot.json
COPY ["AuthService/AuthService.csproj", "AuthService/"]
COPY ["FileService/FileService.csproj", "FileService/"]
COPY ["GatewayApi/GatewayApi.csproj", "GatewayApi/"]
COPY ["AuthService.Tests/AuthService.Tests.csproj", "AuthService.Tests/"]

# Restore all projects
RUN dotnet restore "MicroserviceSystem.sln"

# Copy all source code
COPY . .

# Build all projects
RUN dotnet build "MicroserviceSystem.sln" -c Release --no-restore

# Publish stage - publish each project individually
FROM build AS publish
RUN dotnet publish "AuthService/AuthService.csproj" -c Release -o /app/publish/AuthService --no-restore --no-build
RUN dotnet publish "FileService/FileService.csproj" -c Release -o /app/publish/FileService --no-restore --no-build
RUN dotnet publish "GatewayApi/GatewayApi.csproj" -c Release -o /app/publish/GatewayApi --no-restore --no-build

# Final stage
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=publish /app/publish/AuthService /app/AuthService
COPY --from=publish /app/publish/FileService /app/FileService
COPY --from=publish /app/publish/GatewayApi /app/GatewayApi

# Copy configuration files for AuthService
COPY AuthService/appsettings.json /app/AuthService/appsettings.json
COPY AuthService/appsettings.Development.json /app/AuthService/appsettings.Development.json

# Copy ocelot.json for GatewayApi
COPY GatewayApi/ocelot.json /app/GatewayApi/ocelot.json

# Create startup script
RUN echo '#!/bin/bash\n\
echo "Starting Microservice System..."\n\
echo "Available services:"\n\
echo "1. AuthService - Port 5001"\n\
echo "2. FileService - Port 5002"\n\
echo "3. GatewayApi - Port 5000"\n\
echo ""\n\
echo "To start a specific service, use:"\n\
echo "dotnet /app/AuthService/AuthService.dll"\n\
echo "dotnet /app/FileService/FileService.dll"\n\
echo "dotnet /app/GatewayApi/GatewayApi.dll"\n\
echo ""\n\
echo "Default: Starting GatewayApi on port 5000"\n\
cd /app/GatewayApi && dotnet GatewayApi.dll' > /app/start.sh && chmod +x /app/start.sh

# Set default entry point to GatewayApi
WORKDIR /app
# ENTRYPOINT is now set per service in docker-compose.yml