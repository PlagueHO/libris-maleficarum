# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_VERSION=0.0.0
WORKDIR /src

# Copy project files first for better layer caching
COPY src/Domain/LibrisMaleficarum.Domain.csproj src/Domain/
COPY src/Infrastructure/LibrisMaleficarum.Infrastructure.csproj src/Infrastructure/
COPY src/Orchestration/ServiceDefaults/LibrisMaleficarum.ServiceDefaults.csproj src/Orchestration/ServiceDefaults/
COPY src/Api/LibrisMaleficarum.Api.csproj src/Api/

# Restore dependencies
RUN dotnet restore src/Api/LibrisMaleficarum.Api.csproj

# Copy remaining source code
COPY src/ src/

# Build and publish the API
RUN dotnet publish src/Api/LibrisMaleficarum.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:Version=${BUILD_VERSION} \
    /p:InformationalVersion=${BUILD_VERSION}

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user for running the application
RUN (getent group 1000 || groupadd --gid 1000 appuser) && \
    (getent passwd 1000 || useradd --uid 1000 --gid 1000 --create-home appuser)

COPY --from=build /app/publish .

# Set the non-root user (use numeric ID for compatibility)
USER 1000

EXPOSE 8080

ENTRYPOINT ["dotnet", "LibrisMaleficarum.Api.dll"]
