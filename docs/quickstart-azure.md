---
title: Quickstart Deploy to Azure
description: Provision infrastructure and deploy Libris Maleficarum to Azure with Azure Developer CLI.
ms.date: 2026-05-10
ms.topic: how-to
---

## Overview

Deploy Libris Maleficarum to Azure with Azure Developer CLI (`azd`).

> [!NOTE]
> For local development, use [quickstart-local.md](quickstart-local.md).

## Prerequisites

### Azure Developer CLI

Install the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd):

```bash
# Windows (winget)
winget install Microsoft.Azd

# macOS (Homebrew)
brew install azd

# Linux
curl -fsSL https://aka.ms/install-azd.sh | bash
```

Verify:

```bash
azd version
```

### Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).

### Azure subscription

Use an active Azure subscription with enough quota for the AI model and supporting services in your target region.

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/libris-maleficarum.git
cd libris-maleficarum
```

## 2. Authenticate

Sign in to Azure CLI and Azure Developer CLI:

```bash
az login
azd auth login
```

## 3. Create an environment

```bash
azd env new <env-name>
```

Use a short lowercase environment name such as `dev`, `test`, or `prod`.

## 4. Deploy

Provision and deploy with one command:

```bash
azd up
```

This command provisions infrastructure from `infra/main.bicep` and deploys the application.

## Environment configuration

All infrastructure parameters are read from environment variables in `infra/main.bicepparam`.

### Automatically set by azd

| Variable | Description |
| --- | --- |
| `AZURE_ENV_NAME` | Environment name used for resource naming. |
| `AZURE_LOCATION` | Primary Azure region. |

### Optional

| Variable | Default | Description |
| --- | --- | --- |
| `AZURE_RESOURCE_GROUP` | empty | Use a custom resource group name instead of azd default naming. |
| `AZURE_CREATE_BASTION_HOST` | `false` | Set to `true` to deploy Azure Bastion. |
| `AZURE_STATIC_WEB_APP_LOCATION` | empty | Override static web app location. |
| `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` | empty | Optional custom domain for static web app binding. Leave empty to disable custom domain binding. |
| `API_CONTAINER_IMAGE` | `ghcr.io/plagueho/libris-maleficarum-service:latest` | Backend API container image. |
| `ACCESS_CODE` | empty | Optional API access code for single-user mode. |

Set values before provisioning:

```bash
azd env set AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN "YOUR_CUSTOM_DOMAIN_HERE"
azd env set API_CONTAINER_IMAGE "ghcr.io/plagueho/libris-maleficarum-service:latest"
azd env set ACCESS_CODE "your-access-code"
azd up
```

If you set `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN`, create a DNS CNAME record that points your custom host to the Static Web App default hostname before or during deployment so Azure validation can complete.

## Update and redeploy

Deploy code changes:

```bash
azd deploy
```

Provision only infrastructure changes:

```bash
azd provision
```

Re-run full flow:

```bash
azd up
```

## Tear down

Delete all resources created for the environment:

```bash
azd down
```

Force delete without confirmation:

```bash
azd down --force --purge
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `azd` command not found | Install Azure Developer CLI and restart terminal. |
| Authentication failure | Run `az login` and `azd auth login` again. |
| Quota or capacity errors | Use a different region or request quota increases. |
| Deployment timeout | Run `azd up` again. It resumes from the current state. |
| App endpoint issues | Run `azd env get-values` and verify deployed endpoints and settings. |

## Next steps

* Use [quickstart-local.md](quickstart-local.md) for local development.
* Review design docs in [design/readme.md](design/readme.md).
