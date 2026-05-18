# CI/CD Strategy

This repository uses GitHub Actions workflows in `.github/workflows`.

The workflow model is intentionally split into two layers:

- Entry-point workflows: manually or directly triggered workflows
- Reusable workflows: `workflow_call` workflows invoked only by entry-point workflows

## Entry-Point Workflows

Only the following workflows should be called directly:

- **Continuous Integration (`continuous-integration.yml`)**
- **Continuous Delivery (`continuous-delivery.yml`)**
- **Publish Docs (`publish-docs.yml`)**

### Continuous Integration (`continuous-integration.yml`)

Triggered on push, pull request to `main`, or manual dispatch for code, infra, docs, and build-doc workflow changes.

Current jobs:

- Lint Markdown
- Lint and Publish Bicep
- Build and Publish Frontend App
- Build and Publish Backend Service
- Build Docs

### Continuous Delivery (`continuous-delivery.yml`)

Triggered on push to `main`, push of tags matching `v*`, or manual dispatch.

Current high-level flow:

1. Set build variables
1. Lint and publish Bicep
1. Build and publish frontend app
1. Build and publish backend service image to GHCR
1. Run end-to-end test workflow in a temporary test environment
1. For tag builds (`v*`) only, run production deployment workflow after E2E succeeds

### Publish Docs (`publish-docs.yml`)

Triggered on pushes to `main` that affect docs or docs workflow files, and on manual dispatch.

Current jobs:

- Build docs
- Deploy docs to GitHub Pages

## Reusable Workflows

All other workflows are reusable/orchestrated workflows and are not intended to be directly invoked.

Key reusable workflows include:

- **`set-build-variables.yml`**: Computes build/version outputs
- **`lint-markdown.yml`**: Lints Markdown content
- **`lint-and-publish-bicep.yml`**: Lints Bicep and publishes artifacts
- **`build-and-publish-frontend-app.yml`**: Builds frontend artifacts
- **`build-and-publish-backend-service.yml`**: Builds/tests backend and optionally pushes container image
- **`validate-infrastructure.yml`**: Performs infra validation/what-if checks
- **`provision-infrastructure.yml`**: Provisions Azure infrastructure with `azd provision`
- **`deploy-frontend-app.yml`**: Deploys frontend to Azure Static Web Apps
- **`smoke-test.yml`**: Runs smoke tests against deployed endpoints
- **`delete-infrastructure.yml`**: Cleans up ephemeral environments
- **`e2e-test.yml`**: Orchestrates validation, provision, deploy, smoke, and cleanup for test environments
- **`deploy-production.yml`**: Orchestrates production validate, provision, deploy, and smoke-test stages
- **`build-docs.yml`**: Builds static docs artifacts for publishing

## Environments

### Test (Ephemeral)

- Used by E2E runs in delivery pipeline
- Environment name is generated per run (for example `libmal-<run_id>`)
- Infrastructure is deleted at the end of the E2E workflow

### Production

- Uses GitHub environment `prod`
- Default Azure environment name: `libmal-prod`
- Production deployment is gated behind successful E2E and tag-based release flow (`v*`)

## Secrets and Variables

### Required Azure Auth Secrets

- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_CLIENT_ID`

### Optional Secrets

- `ACCESS_CODE`: Optional API access-code protection in single-user mode
- `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN`: Optional Static Web App custom domain

### Common Variables

- `AZURE_LOCATION`: Azure region used by infra workflows
- `AZURE_ENV_NAME`: Azure Developer CLI environment name where applicable

## Azure Federated Credential Setup

To enable GitHub OIDC authentication for Azure deployments:

1. Register an app in Microsoft Entra ID
1. Add a federated credential for GitHub Actions with issuer `https://token.actions.githubusercontent.com`
1. Scope subject to the repository and environment pattern you use
1. Assign required Azure RBAC roles to the service principal
1. Add `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` as GitHub secrets

This enables passwordless authentication from GitHub Actions to Azure.
