---
title: Quickstart Local Development with Aspire
description: Run Libris Maleficarum locally with Aspire orchestration for API, frontend, and supporting services.
ms.date: 2026-05-10
ms.topic: how-to
---

## Overview

Run Libris Maleficarum locally with .NET Aspire. Aspire orchestrates the backend API, search indexing worker, frontend dev server, and local infrastructure dependencies.

> [!NOTE]
> For Azure deployment, use [quickstart-azure.md](quickstart-azure.md).

## Prerequisites

### .NET SDK 10

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.

```bash
dotnet --version
```

### Node.js and pnpm

Install [Node.js 22 LTS](https://nodejs.org/) or later, then enable pnpm:

```bash
corepack enable
corepack prepare pnpm@latest --activate
pnpm --version
```

### Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and sign in:

```bash
az login
```

If you use multiple subscriptions, set the one you want Aspire to use:

```bash
az account set --subscription <subscription-id-or-name>
```

### Azure subscription quota

Aspire provisions Azure AI resources used by the local development loop. Ensure your subscription has quota for `gpt-5.2-chat` in your target region.

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/libris-maleficarum.git
cd libris-maleficarum
```

## 2. Start the app

Run Aspire AppHost from the service folder:

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj
```

Aspire will:

1. Start local dependencies such as Cosmos DB emulator and Azurite.
1. Start the API service.
1. Start the search indexing worker.
1. Start the frontend Vite dev server from `libris-maleficarum-app`.
1. Provision and connect required Azure AI resources.

First run can take several minutes while resources initialize.

## 3. Open the app

Use the endpoint links shown in the Aspire Dashboard or terminal output:

* Frontend endpoint
* API endpoint
* Aspire Dashboard endpoint

Port values can vary between runs and environments.

## 4. Optional access code protection

You can protect API access in local development by setting `ACCESS_CODE`.

PowerShell:

```powershell
$env:ACCESS_CODE = "your-access-code"
```

Bash:

```bash
export ACCESS_CODE="your-access-code"
```

You can also set it in API user secrets:

```bash
cd libris-maleficarum-service/src/Api
dotnet user-secrets set AccessControl:AccessCode "your-access-code"
```

## 5. Run tests

Backend tests:

```bash
cd libris-maleficarum-service
dotnet test --solution LibrisMaleficarum.slnx
```

Frontend tests:

```bash
cd libris-maleficarum-app
pnpm test
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `dotnet` command not found | Install .NET 10 SDK and re-open the terminal. |
| `pnpm` command not found | Run `corepack enable` and `corepack prepare pnpm@latest --activate`. |
| Azure provisioning or model deployment fails | Confirm subscription access and model quota, then rerun AppHost. |
| Frontend cannot call API | Check resource health and endpoints in Aspire Dashboard. |
| Port conflict errors | Stop conflicting local processes and rerun AppHost. |

## Next steps

* Deploy to Azure with [quickstart-azure.md](quickstart-azure.md).
* Review architecture docs in [design/readme.md](design/readme.md).
