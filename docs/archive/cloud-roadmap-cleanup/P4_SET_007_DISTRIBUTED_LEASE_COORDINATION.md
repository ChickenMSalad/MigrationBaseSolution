# P4.7 Distributed Lease Coordination

This set adds SQL-backed lease coordination primitives for operational work items.

## Added

- `src/Core/Migration.Application/Operational/Leases/OperationalWorkItemLeaseContracts.cs`
- `src/Core/Migration.Infrastructure.Sql/Operational/Leases/SqlOperationalWorkItemLeaseCoordinator.cs`
- `src/Core/Migration.Infrastructure.Sql/Operational/Leases/SqlOperationalWorkItemLeaseCoordinatorOptions.cs`
- `src/Core/Migration.Infrastructure.Sql/Operational/Leases/SqlOperationalWorkItemLeaseCoordinatorServiceCollectionExtensions.cs`
- `database/sql/operational/004_operational_work_item_lease_indexes.sql`

## Purpose

The SQL work-item queue remains the durable source of truth. This set adds the primitives needed for safe distributed worker processing:

- renew a work-item lease while a worker is actively processing it
- release expired leases back into retryable/failed state
- summarize active and expired leases for a run

This is intentionally separate from the existing work-item queue contract to avoid destabilizing P4.4/P4.5/P4.6 while enabling the next executor hardening step.
