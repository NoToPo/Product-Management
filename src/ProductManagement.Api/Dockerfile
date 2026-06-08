# syntax=docker/dockerfile:1
# Multi-stage build for the Product Management API.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer-cached on project files).
COPY ProductManagement.slnx ./
COPY src/ProductManagement.Domain/*.csproj src/ProductManagement.Domain/
COPY src/ProductManagement.Application/*.csproj src/ProductManagement.Application/
COPY src/ProductManagement.Infrastructure/*.csproj src/ProductManagement.Infrastructure/
COPY src/ProductManagement.Api/*.csproj src/ProductManagement.Api/
RUN dotnet restore src/ProductManagement.Api/ProductManagement.Api.csproj

# Build + publish.
COPY . .
RUN dotnet publish src/ProductManagement.Api/ProductManagement.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductManagement.Api.dll"]
