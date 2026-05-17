# P1 Set 021 — Cloud Diagnostics Validation Expansion

## Purpose

P1 Set 021 updates the cloud diagnostics validation script so it covers the newer P1 endpoints added after Set 011.

This is tooling-only and does not change application runtime behavior.

## Replaced file

- `tools/cloud/validate-cloud-diagnostics.ps1`

## Added docs

- `docs/cloud-roadmap-cleanup/P1_SET_021_CLOUD_DIAGNOSTICS_EXPANSION.md`

## Newly validated HTTP endpoints

- `GET /health/live`
- `GET /health/ready`
- `GET /health/cloud`
- `GET /api/cloud/telemetry/correlation`
- `GET /api/cloud/audit/event-contract`
- `GET /api/cloud/auth/policy-plan`
- `GET /api/cloud/auth/configuration`

## Also validates docs/tooling files

- `tools/cloud/generate-azure-resource-names.ps1`
- `tools/cloud/show-promotion-checklist.ps1`
- `docs/azure/AZURE_RESOURCE_NAMING.md`
- `docs/azure/AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md`
- `docs/azure/FRONTEND_AUTH_BOOTSTRAP.md`
- `docs/azure/BACKEND_AUTH_SCAFFOLD.md`

## Validation

Template/docs only:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp
```

Full endpoint validation after starting Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -BaseUrl http://localhost:5173
```
