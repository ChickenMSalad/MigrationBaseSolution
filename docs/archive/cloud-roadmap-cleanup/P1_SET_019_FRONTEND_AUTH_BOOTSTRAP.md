# P1 Set 019 — Frontend Auth Bootstrap Plan

## Purpose

P1 Set 019 prepares the Admin Web frontend for future Entra ID/OIDC login without enforcing login yet.

This set does not install auth libraries and does not change routing behavior.

## Added files

- `src/Admin/Migration.Admin.Web/src/auth/frontendAuthConfig.ts`
- `src/Admin/Migration.Admin.Web/src/auth/index.ts`
- `src/Admin/Migration.Admin.Web/src/api/authReadiness.ts`
- `src/Admin/Migration.Admin.Web/.env.auth.example`
- `docs/azure/FRONTEND_AUTH_BOOTSTRAP.md`
- `docs/cloud-roadmap-cleanup/P1_SET_019_FRONTEND_AUTH_BOOTSTRAP.md`

## Validation

From `src/Admin/Migration.Admin.Web` run:

```powershell
npm run build
```
