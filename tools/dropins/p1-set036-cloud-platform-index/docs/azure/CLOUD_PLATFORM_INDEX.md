# Cloud Platform Index

## Purpose

This index ties together the cloud platform planning, diagnostics, deployment, and release-readiness assets added during P1.

## Runtime diagnostics

| Area | Endpoint |
|---|---|
| Environment | `GET /api/cloud/environment` |
| Workspace context | `GET /api/workspace/context` |
| Workspace storage plan | `GET /api/workspace/storage-plan` |
| Credential provider plan | `GET /api/cloud/credential-provider-plan` |
| Artifact storage plan | `GET /api/cloud/artifact-storage-plan` |
| Queue provider plan | `GET /api/cloud/queue-provider-plan` |
| Deployment profile | `GET /api/cloud/deployment-profile` |
| Configuration audit | `GET /api/cloud/configuration-audit` |
| Cloud readiness | `GET /api/cloud/readiness` |
| Telemetry correlation | `GET /api/cloud/telemetry/correlation` |
| Audit event contract | `GET /api/cloud/audit/event-contract` |
| Auth policy plan | `GET /api/cloud/auth/policy-plan` |
| Auth configuration | `GET /api/cloud/auth/configuration` |

## Health endpoints

| Area | Endpoint |
|---|---|
| Liveness | `GET /health/live` |
| Readiness | `GET /health/ready` |
| Cloud health | `GET /health/cloud` |

## Local validation scripts

| Purpose | Script |
|---|---|
| Cloud diagnostics validation | `tools/cloud/validate-cloud-diagnostics.ps1` |
| Azure resource naming | `tools/cloud/generate-azure-resource-names.ps1` |
| Promotion checklist helper | `tools/cloud/show-promotion-checklist.ps1` |
| Generate appsettings from outputs | `tools/cloud/new-cloud-appsettings-from-outputs.ps1` |
| Publish cloud artifacts | `tools/build/publish-cloud-artifacts.ps1` |
| Generate release manifest | `tools/release/new-release-manifest.ps1` |
| Validate release readiness | `tools/release/validate-release-readiness.ps1` |

## Azure scaffolds

| Area | Path |
|---|---|
| Deployment orchestration | `deploy/azure/deploy-cloud-scaffold.ps1` |
| Storage | `deploy/azure/storage` |
| Key Vault | `deploy/azure/key-vault` |
| App Service | `deploy/azure/app-service` |
| Queue Executor Container Apps Job | `deploy/azure/container-apps-job` |
| Managed Identity RBAC | `deploy/azure/rbac` |

## Container scaffolds

| Area | Path |
|---|---|
| Admin API Dockerfile | `deploy/docker/AdminApi.Dockerfile` |
| Queue Executor Dockerfile | `deploy/docker/QueueExecutor.Dockerfile` |
| Local compose | `deploy/docker/docker-compose.local.yml` |

## GitHub Actions

| Workflow | Path |
|---|---|
| Platform validation | `.github/workflows/migration-platform-validation.yml` |
| Azure deployment scaffold | `.github/workflows/azure-deployment-scaffold.yml` |

## Recommended local checkpoint

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj

cd .\src\Admin\Migration.Admin.Web
npm run build
cd ..\..\..

powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp -Strict
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 -Version 0.1.0-dev -EnvironmentName dev
powershell -ExecutionPolicy Bypass -File .\tools\release\validate-release-readiness.ps1 -RequirePublishArtifacts -Strict
```

## P1 status

P1 has established:

- cloud diagnostic contracts
- health endpoints
- auth planning contracts
- telemetry/audit planning contracts
- Azure infrastructure scaffolds
- CI validation scaffolds
- artifact publishing
- release manifest and validation tooling

The repo is now ready to move from cloud planning/scaffold work into implementation-heavy P2 work.
