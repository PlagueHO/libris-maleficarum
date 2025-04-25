# Libris Maleficarum

**Libris Maleficarum** (Latin for "Book of Witchcraft") is an AI-enhanced campaign management and narrative tool designed for tabletop RPGs. While it's currently aimed at providing campaign, session, and character management, its flexible design and architecutre will allow it to evolve into a general TTRPG assistant platform.

---

## Overview

Libris Maleficarum is built with modern, scalable technologies:

- A **.NET 8** backend using **Aspire.NET** for lightweight API scaffolding.
- **Entity Framework Core** (Cosmos DB provider) alongside the **Repository Pattern** for data access.
- A single Azure Cosmos DB container employing a composite partition key approach (using synthetic properties if necessary).
- A **React** + **TypeScript** frontend that communicates with the backend via RESTful APIs.
- A stubbed identity service for future multi-user support and eventual integration with Entra ID CIAM.

---

## Architecture Overview

### High-Level Components

- **Backend (.NET 8 with Aspire.NET):**
  - API endpoints and business logic.
  - Entity Framework Core for Cosmos DB access.
  - Repository layer abstracting data operations.
  - Stubbed identity (via `IUserContextService`) to handle multi-user scenarios.

- **Frontend (React + TypeScript):**
  - Modern, responsive UI.
  - Communication with the backend using Axios.
  - Future integration with MSAL.js for authentication.

- **Data Storage (Azure Cosmos DB):**
  - A single container ("LibrisMaleficarumContainer") that stores all entities.
  - Composite partition key defined by the properties `UserId` and `CampaignId` (or a synthetic property combining them).
  - Documents include a common `EntityType` field for type discrimination (e.g., "Campaign", "Session", etc.).

- **Future Enhancements:**
  - Integration with Entra ID CIAM for secure, multi-user authentication.
  - Expansion of AI features to support dynamic narrative generation and analytics.

### High-Level Architecture Diagram

+-----------------------------------------+ | Frontend (React + TS) | | - Axios for API communication | +---------------------+-------------------+ │ ▼ +-----------------------------------------+ | API Gateway / .NET 8 API | | (Aspire.NET, EF Core, Repositories) | +---------------------+-------------------+ │ +----------------+----------------+ │ │ +--------▼---------+ +--------▼----------+ | Domain & | | Identity & | | Application | | Authentication | | Services (Repos) | | (Stub now, future) | +--------+---------+ +--------+----------+ │ │ ▼ ▼ +-----------------------------------------------+ | Cosmos DB (Single Container) | | • Documents contain: | | - UserId | | - CampaignId (Composite Partition Key) | | - EntityType (Campaign/Session/etc.) | +-----------------------------------------------+

---

## Technology Stack

- **Backend:**
  - **.NET 8** & **Aspire.NET**
  - **Entity Framework Core** (Cosmos DB provider)
  - **Repository Pattern**
  - **IUserContextService** (stubbed; replace with Entra ID CIAM integration later)

- **Frontend:**
  - **React** with **TypeScript** (Create React App)
  - **Axios** for HTTP communication
  - Optionally **React Router** for multi-page interface

- **Data Storage:**
  - **Azure Cosmos DB** using a single container and composite partition key strategy

- **Deployment & DevOps:**
  - Local development using the Cosmos DB Emulator
  - Dockerization for cloud deployment (Azure App Service or AKS)
  - CI/CD via GitHub Actions or Azure DevOps

---

## Data Model & Cosmos DB Design

All TTRPG-related entities are stored in one container.

### Document Structure

Each document includes:

- `id`: Unique document identifier.
- `UserId`: Identifies the owner (supports multi-user capabilities).
- `CampaignId`: For campaign documents, this is the same as the campaign's `id`. For sub-entities like Sessions, Characters, and Events, this references the parent campaign.
- `EntityType`: Type discriminator (e.g., "Campaign", "Session", "Character", "Event").
- *(Optionally)* `PartitionKey`: A synthetic property combining `UserId` and `CampaignId` (e.g., `"{UserId}:{CampaignId}"`) if needed.

### Example: Campaign Document

```json
{
  "id": "campaign-123",
  "UserId": "user-abc",
  "CampaignId": "campaign-123",
  "EntityType": "Campaign",
  "Name": "The Dark Coven",
  "Description": "A campaign of intrigue and forbidden magic.",
  "CreatedDate": "2023-10-05T10:23:00Z"
}
