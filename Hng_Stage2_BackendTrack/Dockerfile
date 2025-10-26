# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./Hng_Stage2_BackendTrack.csproj"
RUN dotnet build "./Hng_Stage2_BackendTrack.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "./Hng_Stage2_BackendTrack.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hng_Stage2_BackendTrack.dll"]
