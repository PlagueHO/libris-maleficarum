# CI/CD Strategy

Continuous Integration and Continuous Deployment (CI/CD) will be implemented using **GitHub Actions**, stored in the `.github\workflows` folder:

## GitHub Actions Workflows

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

## Environments

### Test

- **Description:** Used for testing infrastructure and application deployments.
- **Azure Location:** `eastus2`
- **Azure Resource Base Name:** `dsr-libris-maleficarum`

Additional environments can be added by extending the workflows and providing the required variables and secrets.

## Secrets and Variables

### Secrets

The following secrets are required for the workflows to authenticate with Azure:

- **`AZURE_TENANT_ID`**: The Azure tenant ID for authentication.
- **`AZURE_SUBSCRIPTION_ID`**: The Azure subscription ID for authentication.
- **`AZURE_CLIENT_ID`**: The Azure client ID for authentication.

### Variables

The following variables are required for the workflows:

- **`AZURE_ENV_NAME`**: The base name of the Azure resources (e.g., `dsr-libris-maleficarum`)
- **`AZURE_LOCATION`**: The Azure region for deployment (e.g., `eastus2`).

## Creating the Azure Application Federation Credential

To allow GitHub Actions workflows to connect to the Azure subscription and deploy resources, follow these steps:

1. **Register an Azure AD Application**:
   - Navigate to the Azure portal and go to **Azure Entra ID** > **App registrations**.
   - Click **New registration** and provide a name (e.g., `GitHubActionsApp`).
   - Set the **Supported account types** to "Accounts in this organizational directory only".
   - Click **Register**.

1. **Create a Federated Credential**:
   - Go to **Certificates & secrets** > **Federated credentials**.
   - Click **Add credential** and configure:
     - **Issuer**: `https://token.actions.githubusercontent.com`
     - **Subject Identifier**: `repo:<GitHubOrg>/<GitHubRepo>:environment:<EnvironmentName>`
     - **Audience**: `api://AzureADTokenExchange`
   - Save the credential.

1. **Assign Roles to the Application**:
   - Navigate to the Azure subscription and go to **Access control (IAM)**.
   - Click **Add role assignment** and assign the `Contributor` role to the registered application.

1. **Add Secrets to GitHub**:
   - Copy the `Application (client) ID` and `Directory (tenant) ID` from the Azure Entra ID application.
   - Add these as secrets (`AZURE_CLIENT_ID` and `AZURE_TENANT_ID`) in the GitHub repository settings.
   - Add the Azure subscription ID as `AZURE_SUBSCRIPTION_ID`.

Once configured, the GitHub Actions workflows will be able to authenticate with Azure and deploy resources securely.
