# Authentication

Libris Maleficarum supports three authentication modes that control how users are identified and how access to the API is protected. The mode you choose depends on whether you are running the application for a single user or multiple users, and how much protection you need.

| Mode | Users | Use case |
| --- | --- | --- |
| Anonymous | Single | Local development, private networks with no access control needed |
| Anonymous + Access Code | Single | Personal deployments with lightweight protection and no user accounts |
| Entra ID | Multi | Shared team or organizational deployments requiring individual identity |

## Current support status

| Mode | Current status | Notes |
| --- | --- | --- |
| Anonymous | Supported | Default mode when no Entra ID or access code configuration is present. |
| Anonymous + Access Code | Supported | End-to-end support exists in frontend, API middleware, and infrastructure variable flow using `ACCESS_CODE`. |
| Entra ID (multi-user) | Partially supported | Runtime support exists in frontend and API when Entra values are provided, but automated infrastructure and deployment workflow support is not yet implemented. |

## Anonymous mode

Anonymous mode is the default. No environment variables or configuration are required. All API requests are accepted without any access check, and every request is attributed to a single shared identity: `_anonymous`.

All documents, sessions, and suggestions are stored in Cosmos DB under the `_anonymous` user partition. There is no way to distinguish between different browser sessions or end users.

**When to use:** Local development, a personal instance on a private network, or any scenario where you do not need access control.

### Configure for local development

No configuration is needed. Start the app normally and it runs in anonymous mode.

If a previously set access code is present in your user secrets, remove it:

```bash
cd libris-maleficarum-service/src/Api
dotnet user-secrets remove AccessControl:AccessCode
```

Ensure the `ACCESS_CODE` environment variable is not set, then run:

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

### Configure for Azure deployment

Do not set the `ACCESS_CODE` GitHub Actions secret or `azd` environment variable. Ensure no value is present:

```bash
azd env set ACCESS_CODE ""
```

Then deploy as normal:

```bash
azd up
```

---

## Anonymous + Access Code mode

Anonymous + Access Code mode adds a lightweight password gate to a single-user deployment. When an access code is configured:

1. The frontend checks `GET /api/config/access-status` on startup.
1. If an access code is required, a modal prompts for the code before the app loads.
1. The code is stored in browser session storage (cleared when the tab closes).
1. Every API request includes the code in the `X-Access-Code` header.
1. The backend `AccessCodeMiddleware` validates the header using constant-time comparison and returns `401 Unauthorized` if the code is missing or incorrect.

Health and status endpoints (`/health`, `/alive`, `/api/config/access-status`) are exempt from the check and always accessible.

The user identity remains `_anonymous`. This mode protects access to a single-user instance, and it does not support multiple users.

> **Security note:** Access Code mode is a lightweight access gate, not a high-security authentication mechanism. For production multi-user deployments, use Entra ID mode.

### Configure for local development

**Option 1 — User secrets (recommended for development)**

```bash
cd libris-maleficarum-service/src/Api
dotnet user-secrets set AccessControl:AccessCode "your-access-code"
```

**Option 2 — `appsettings.Development.json`**

Add or update the `AccessControl` section in `libris-maleficarum-service/src/Api/appsettings.Development.json`:

```json
{
  "AccessControl": {
    "AccessCode": "your-access-code"
  }
}
```

> Do not commit access codes to source control. Prefer user secrets for local development.

**Option 3 — Environment variable**

On Linux/macOS:

```bash
export ACCESS_CODE="your-access-code"
```

On Windows (PowerShell):

```powershell
$env:ACCESS_CODE = "your-access-code"
```

### Configure for Azure deployment

The access code flows from a GitHub Actions secret into the deployed Container App environment variable.

1. Go to your GitHub repository **Settings** → **Secrets and variables** → **Actions**.
1. Click **New repository secret** (or add to the `prod` environment).
1. Set **Name** to `ACCESS_CODE` and **Value** to your desired access code.

The secret flows through the pipeline: `continuous-delivery.yml` → `deploy-production.yml` → `provision-infrastructure.yml` → Bicep `accessCode` parameter → Container App `ACCESS_CODE` environment variable.

Alternatively, set the value in the `azd` environment before deploying:

```bash
azd env set ACCESS_CODE "your-access-code"
azd up
```

To remove access code protection from an existing deployment, clear the value and redeploy:

```bash
azd env set ACCESS_CODE ""
azd provision
```

---

## Entra ID mode

Entra ID mode uses Microsoft Entra ID to authenticate users. Each user signs in with their organizational account, and requests are authorized with bearer tokens.

### What works today

The application runtime already has Entra-aware behavior:

1. The frontend can run in Entra mode when `ENTRA_CLIENT_ID` and `ENTRA_TENANT_ID` are provided.
1. The frontend uses MSAL and requests the `api://libris-maleficarum-api/access_as_user` scope.
1. The API switches to Entra JWT validation when `AzureAd:ClientId` is configured.

### What is not yet supported

The following parts are not yet implemented in this repository:

1. No Entra-specific infrastructure templates in `infra` (for app registration provisioning).
1. No Entra pre-provision hooks for `azd` that create or manage app registrations.
1. No CI/CD workflow plumbing for Entra-specific environment values such as `ENABLE_ENTRA_AUTH`, `AZURE_AD_API_CLIENT_ID`, or `AZURE_AD_SPA_CLIENT_ID`.
1. No fully documented and validated end-to-end `azd` deployment path for multi-user Entra mode.

### Interim manual configuration (advanced)

If you want to test the current runtime-level support before infrastructure automation is added, you can manually provide Entra values:

- API configuration keys under `AzureAd` (`ClientId`, `TenantId`, `Instance`, `Audience`, `Scopes`).
- AppHost configuration keys under `EntraId` (`ClientId`, `TenantId`, optional `Audience`) so values are propagated to API and frontend.

This path is currently manual and should be treated as implementation-in-progress.

---

## Comparing modes

| Capability | Anonymous | Anonymous + Access Code | Entra ID |
| --- | --- | --- | --- |
| No configuration required | Yes | No | No |
| Protects against unauthorized access | No | Yes | Yes |
| Supports multiple users | No | No | Yes |
| Per-user data isolation | No | No | Planned |
| Requires Azure AD tenant | No | No | Yes |
| Works locally (Aspire) | Yes | Yes | Partial (manual configuration only) |
| Recommended for production | No | Personal use only | Not yet (until automation and end-to-end validation are complete) |
