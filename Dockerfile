# Backend Dockerfile for OmniFlow API
# Multi-stage build for optimized image size

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["OmniFlow.WebApi/OmniFlow.WebApi.csproj", "OmniFlow.WebApi/"]
COPY ["OmniFlow.Application/OmniFlow.Application.csproj", "OmniFlow.Application/"]
COPY ["OmniFlow.Domain/OmniFlow.Domain.csproj", "OmniFlow.Domain/"]
COPY ["OmniFlow.Infrastructure/OmniFlow.Infrastructure.csproj", "OmniFlow.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "OmniFlow.WebApi/OmniFlow.WebApi.csproj"

# Copy all source code
COPY OmniFlow.WebApi/ ./OmniFlow.WebApi/
COPY OmniFlow.Application/ ./OmniFlow.Application/
COPY OmniFlow.Domain/ ./OmniFlow.Domain/
COPY OmniFlow.Infrastructure/ ./OmniFlow.Infrastructure/

# Build the application
WORKDIR "/src/OmniFlow.WebApi"
RUN dotnet build "OmniFlow.WebApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "OmniFlow.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Expose port
EXPOSE 8080
EXPOSE 8081

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set entry point
ENTRYPOINT ["dotnet", "OmniFlow.WebApi.dll"]