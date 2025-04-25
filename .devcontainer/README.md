# GitHub Codespaces for Libris Maleficarum

This repository is configured for GitHub Codespaces development.

## Features

- .NET 8 SDK and runtime
- Node.js 18.x for React/TypeScript frontend
- Azure CLI for cloud integration
- Pre-installed VS Code extensions for .NET, Azure, Docker, and JavaScript/TypeScript
- Ports auto-forwarding for backend and frontend development

## Getting Started

1. Open this repository in GitHub Codespaces.
2. The container will build and install dependencies for both backend and frontend.
3. Use the VS Code terminal to run:
   - `dotnet run` for backend
   - `npm start` (from `frontend` directory) for frontend

## Customization

- Edit `.devcontainer/devcontainer.json` to add tools or change settings.
- Add secrets and environment variables to `.devcontainer/devcontainer.env`.
