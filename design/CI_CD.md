# CI/CD Strategy

Continuous Integration and Continuous Deployment (CI/CD) will be implemented using **GitHub Actions**, stored in the `.github` folder:

- **Build & Test:** Automated builds, unit tests, linting, and security scans.
- **Deployment:** Automated deployments to Azure Static Web Apps (frontend) and Azure Container Apps (backend APIs).
- **Infrastructure Deployment:** Automated deployment of Azure resources using Bicep templates.
- **Secrets Management:** Securely managed via Azure Key Vault integration.
- **Monitoring & Logging:** Integrated with Azure Application Insights and Log Analytics for operational visibility.
