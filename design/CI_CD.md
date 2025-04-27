# CI/CD Strategy

Continuous Integration and Continuous Deployment (CI/CD) will be implemented using **GitHub Actions**, stored in the `.github\workflows` folder:

## Workflows

### Continuous Integration (`continuous-integration.yml`)

Triggered on pull requests to the `main` branch:

- **Lint and Publish Bicep:** Lint Bicep templates and publish as artifacts.

### Continuous Deployment (`continuous-deployment.yml`)

Triggered on pushes to the `main` branch, tags (`v*`), or manually:

- **Set Build Variables:** Determine build version using GitVersion.
- **Lint and Publish Bicep:** Lint Bicep templates and publish as artifacts.
- **Validate Infrastructure (Test):** Validate Azure infrastructure deployment using Bicep templates.
- **Deploy Infrastructure (Test):** Deploy Azure infrastructure using validated Bicep templates.

### Reusable Workflows

These workflows are called by the main workflows above:

- **Set Build Variables (`set-build-variables.yml`):** Determines and outputs the build version.
- **Lint and Publish Bicep (`lint-and-publish-bicep.yml`):** Lints Bicep templates and publishes them as artifacts.
- **Validate Infrastructure (`validate-infrastructure.yml`):** Performs a "what-if" validation of Azure infrastructure deployment.
- **Deploy Infrastructure (`deploy-infrastructure.yml`):** Deploys Azure infrastructure resources using Bicep templates.

## Additional Capabilities

- **Secrets Management:** Securely managed via Azure Key Vault integration.
- **Monitoring & Logging:** Integrated with Azure Application Insights and Log Analytics for operational visibility.
