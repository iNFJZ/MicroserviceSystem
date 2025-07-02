# Multi-stage build for Microservice System
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and all project files first
COPY MicroserviceSystem.sln ./
COPY AuthService/AuthService.csproj AuthService/
COPY FileService/FileService.csproj FileService/
COPY GatewayApi/GatewayApi.csproj GatewayApi/
COPY EmailService/EmailService.csproj EmailService/
COPY GrpcGreeter/GrpcGreeter.csproj GrpcGreeter/

# Restore dependencies for all projects
RUN dotnet restore "AuthService/AuthService.csproj" && \
    dotnet restore "FileService/FileService.csproj" && \
    dotnet restore "GatewayApi/GatewayApi.csproj" && \
    dotnet restore "EmailService/EmailService.csproj" && \
    dotnet restore "GrpcGreeter/GrpcGreeter.csproj"

# Copy the rest of the source code
COPY . .

# Build all projects
RUN dotnet build "AuthService/AuthService.csproj" -c Release --no-restore && \
    dotnet build "FileService/FileService.csproj" -c Release --no-restore && \
    dotnet build "GatewayApi/GatewayApi.csproj" -c Release --no-restore && \
    dotnet build "EmailService/EmailService.csproj" -c Release --no-restore && \
    dotnet build "GrpcGreeter/GrpcGreeter.csproj" -c Release --no-restore

# Publish all projects
RUN dotnet publish "AuthService/AuthService.csproj" -c Release -o /app/publish/AuthService && \
    dotnet publish "FileService/FileService.csproj" -c Release -o /app/publish/FileService && \
    dotnet publish "GatewayApi/GatewayApi.csproj" -c Release -o /app/publish/GatewayApi && \
    dotnet publish "EmailService/EmailService.csproj" -c Release -o /app/publish/EmailService && \
    dotnet publish "GrpcGreeter/GrpcGreeter.csproj" -c Release -o /app/publish/GrpcGreeter

# Final stage: Copy published applications and configuration files
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=build /app/publish/AuthService /app/AuthService
COPY --from=build /app/publish/FileService /app/FileService
COPY --from=build /app/publish/GatewayApi /app/GatewayApi
COPY --from=build /app/publish/EmailService /app/EmailService
COPY --from=build /app/publish/GrpcGreeter /app/GrpcGreeter

# Copy configuration files for AuthService
COPY AuthService/appsettings.json /app/AuthService/appsettings.json
COPY AuthService/appsettings.Development.json /app/AuthService/appsettings.Development.json

# Copy ocelot.json for GatewayApi
COPY GatewayApi/ocelot.json /app/GatewayApi/ocelot.json

# Create startup script
RUN echo '#!/bin/bash\
echo "Starting Microservice System..."\
echo "Available services:"\
echo "1. AuthService - Port 5001"\
echo "2. FileService - Port 5002"\
echo "3. GatewayApi - Port 5000"\
echo ""\
echo "To start a specific service, use:"\
echo "dotnet /app/AuthService/AuthService.dll"\
echo "dotnet /app/FileService/FileService.dll"\
echo "dotnet /app/GatewayApi/GatewayApi.dll"\
echo ""\
echo "Default: Starting GatewayApi on port 5000"\
cd /app/GatewayApi && dotnet GatewayApi.dll' > /app/start.sh && chmod +x /app/start.sh

# Set default entry point to GatewayApi
WORKDIR /app
# ENTRYPOINT is now set per service in docker-compose.yml