# [Optional] Extend from the official .NET 8 dev container image
FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# Install additional tools if needed
RUN apt-get update && \
    apt-get install -y azure-cli && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
