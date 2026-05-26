# Runtime Deployment Handoff Checklist

Use this checklist for SQL + Service Bus runtime deployments.

## Pre-deployment

- [ ] Working tree is clean.
- [ ] Deployment commit SHA is recorded.
- [ ] `tools/validate-p7.8a-runtime-contract-cleanup.ps1` passes.
- [ ] `tools/validate-p7.8b-sql-baseline-consolidation.ps1` passes.
- [ ] `tools/validate-p7.8c-runtime-appsettings-normalization.ps1` passes.
- [ ] `tools/validate-p7.8d-runtime-deployment-smoke-handoff.ps1` passes.
- [ ] `tools/validate-p7.8e-local-azure-parity.ps1` passes.
- [ ] `tools/validate-p7.8f-runtime-code-contract-guardrails.ps1` passes.
- [ ] `tools/validate-p7.8g-runtime-release-gates.ps1` passes.
- [ ] `tools/validate-p7.8h-legacy-runtime-quarantine.ps1` passes.
- [ ] `tools/validate-p7.8i-cleanup-package-quality-gates.ps1` passes.

## SQL validation

- [ ] Target database contains `migration.WorkItems`.
- [ ] Target database contains `migration.ManifestRows`.
- [ ] `migration.WorkItems.WorkItemId` is `bigint` identity.
- [ ] `migration.WorkItems.ManifestRowId` is `bigint` nullable.
- [ ] Legacy GUID-era tables are not used by dispatcher/executor.
- [ ] Stored procedure parameters for runtime work items use `bigint` work item ids.

## App settings validation

- [ ] Dispatcher uses `SqlServiceBusDispatcher__*` canonical keys.
- [ ] Executor uses `SqlServiceBusExecutor__*` canonical keys.
- [ ] Executor uses `SqlOperationalWorkItemQueue__SchemaName=migration`.
- [ ] Executor uses `SqlOperationalWorkItemQueue__WorkItemsTableName=WorkItems`.
- [ ] `SqlOperationalMigrationJobExecutor__Enabled=true` is set for runtime execution smoke.
- [ ] Stale duplicate keys are documented or removed.

## Deployment

- [ ] Dispatcher artifact was freshly published from clean commit.
- [ ] Executor artifact was freshly published from clean commit.
- [ ] Executor restarted after deployment.
- [ ] Dispatcher restarted after smoke item enqueue.

## Smoke verification

- [ ] Smoke work item was inserted into `migration.WorkItems`.
- [ ] Dispatcher changed smoke work item to `Dispatched`.
- [ ] Service Bus active count increased or executor consumed the message.
- [ ] Executor log shows work item id and migration job name.
- [ ] Final SQL state is recorded.
- [ ] Any failure is a known business/runtime provider failure, not schema/config/deserialization failure.

## Handoff

- [ ] Final queue counts are recorded.
- [ ] Final SQL work item state is recorded.
- [ ] App setting snapshot is attached.
- [ ] Schema validation output is attached.
- [ ] Smoke logs are attached.
- [ ] Known issues are documented.

