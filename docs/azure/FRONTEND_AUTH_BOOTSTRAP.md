# Frontend Auth Bootstrap

## Goal

Prepare the React/Vite Admin Web frontend for future Entra ID authentication.

## Current state

This is planning/configuration only.

No login is enforced yet.

## Example `.env.local`

```text
VITE_AUTH_ENABLED=false
VITE_AUTH_AUTHORITY=https://login.microsoftonline.com/<tenant-id>/v2.0
VITE_AUTH_CLIENT_ID=<frontend-spa-client-id>
VITE_AUTH_AUDIENCE=api://<admin-api-client-id>
VITE_AUTH_SCOPES=api://<admin-api-client-id>/migration.read api://<admin-api-client-id>/migration.write
VITE_AUTH_REDIRECT_URI=http://localhost:5173
VITE_AUTH_POST_LOGOUT_REDIRECT_URI=http://localhost:5173
```

## Frontend readiness helper

The frontend can read config via:

```ts
import { getFrontendAuthConfig, getFrontendAuthWarnings } from './auth';
```

## Backend alignment

Compare frontend config with:

```http
GET /api/cloud/auth/configuration
GET /api/cloud/auth/policy-plan
```

## Future implementation

A later set should add MSAL or another OIDC client and use these config values as the single frontend auth source of truth.
