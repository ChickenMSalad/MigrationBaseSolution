# Azure Storage Infrastructure Plan

## Purpose

Cloud storage should be provisioned separately from application hosting so it can be shared by:

- Admin API
- Queue Executor
- future workers
- audit/event persistence
- artifact storage
- control-plane state

## Planned resources

| Resource | Purpose |
|---|---|
| Storage account | Blob + Queue services |
| Artifact container | manifests, mappings, taxonomy, generated artifacts |
| Control-plane container | projects, runs, execution state |
| Audit container | future audit event persistence |
| Queue | migration run dispatch |

## Security expectations

- public blob access disabled
- HTTPS required
- managed identity preferred
- RBAC assignments handled separately
- no connection strings in production config
