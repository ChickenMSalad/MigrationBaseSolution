# Post-P2 Cleanup Set 005 — Source Inventory

## Purpose

This set adds a maintenance script that inventories source files added or affected by the P2 cloud/control-plane work.

It focuses on these source areas:

```text
src/Migration.ControlPlane/Auth
src/Migration.ControlPlane/Audit
src/Migration.ControlPlane/Operations
src/Migration.ControlPlane/Queues
src/Migration.ControlPlane/Telemetry
src/Migration.Admin.Api/Endpoints
src/Admin/Migration.Admin.Web/src/api
```

It does not modify source code.

## Why this matters

Before P3, the repo should have a clear source map of:

- contracts
- services
- registration extensions
- endpoint extensions
- frontend API clients
- likely comment-review candidates
- likely organization/refactor candidates

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-source-structure.ps1
```

Output:

```text
docs/post-p2-cleanup/P2_SOURCE_STRUCTURE_INVENTORY_REPORT.md
```

## Recommendation

Use the report to decide whether the next cleanup set should:

1. organize service registration into fewer extension methods
2. add XML comments to governance/safety contracts
3. split oversized endpoint areas
4. leave the source structure alone until P3 begins
