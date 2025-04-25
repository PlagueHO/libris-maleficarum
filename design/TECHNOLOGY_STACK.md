# Technology Stack

- **Backend:**
  - **.NET 8** & **Aspire.NET**
  - **Entity Framework Core** (Cosmos DB provider)
  - **Repository Pattern**
  - **IUserContextService** (stubbed; replace with Entra ID CIAM integration later)

- **Frontend:**
  - **React** with **TypeScript** (Create React App)
  - **Redux** for state management
  - **Axios** for HTTP communication
  - Optionally **React Router** for multi-page interface

- **Data Storage:**
  - **Azure Cosmos DB** using multiple containers for hierarchical, flexible entity storage and a dedicated container for document hierarchy.

- **Deployment & DevOps:**
  - Local development using the Cosmos DB Emulator
  - Dockerization for cloud deployment (Azure App Service or AKS)
  - CI/CD via GitHub Actions or Azure DevOps
