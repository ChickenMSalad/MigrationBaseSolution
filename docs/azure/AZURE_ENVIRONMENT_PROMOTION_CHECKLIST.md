# Azure Environment Promotion Checklist

## Purpose

This checklist defines the minimum validation gates before promoting MigrationBaseSolution from one environment to the next.

## Environments

- local-dev
- dev
- test
- prod

## Local-dev checklist

- [ ] Admin API builds successfully
- [ ] Queue Executor builds successfully
- [ ] Frontend builds successfully
- [ ] `/api/cloud/environment` returns `localDevelopment`
- [ ] `/api/cloud/configuration-audit` returns `localDevelopment`
- [ ] `/api/cloud/readiness` returns expected local warnings only
- [ ] Local user-secrets are configured
- [ ] Local control-plane root exists
- [ ] Smoke tests pass

## Dev checklist

- [ ] App Service or Container App target selected
- [ ] Storage account name selected
- [ ] Artifact container name selected
- [ ] Control-plane storage root selected
- [ ] Queue provider selected
- [ ] Queue name selected
- [ ] Key Vault URI selected
- [ ] Managed Identity plan selected
- [ ] Auth authority/audience identified
- [ ] Workspace ID is explicit and not `default`
- [ ] `/api/cloud/configuration-audit` has no missing required cloud keys
- [ ] `/api/cloud/readiness` warnings are understood and accepted

## Test checklist

- [ ] Dev checklist completed
- [ ] Auth is enabled
- [ ] Tenant enforcement is enabled
- [ ] Private networking decision is documented
- [ ] Diagnostics are enabled
- [ ] Health probes are enabled
- [ ] Queue poison/dead-letter strategy is documented
- [ ] Artifact lifecycle/retention is documented
- [ ] Rollback plan exists
- [ ] Test migration run executed successfully

## Prod checklist

- [ ] Test checklist completed
- [ ] Production Key Vault exists
- [ ] Production storage account exists
- [ ] Production artifact container exists
- [ ] Production queue exists
- [ ] Managed Identity has least-privilege access
- [ ] Auth tenant/app registration reviewed
- [ ] Monitoring alerts configured
- [ ] Backup/retention plan reviewed
- [ ] Release approval completed
- [ ] Final `/api/cloud/readiness` accepted

## Diagnostic endpoints

Use these endpoints during every promotion:

```http
GET /api/cloud/environment
GET /api/cloud/deployment-profile
GET /api/cloud/configuration-audit
GET /api/cloud/readiness
GET /api/workspace/context
GET /api/workspace/storage-plan
GET /api/cloud/credential-provider-plan
GET /api/cloud/artifact-storage-plan
GET /api/cloud/queue-provider-plan
```
