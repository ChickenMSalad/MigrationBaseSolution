# Queue Executor Container Apps Job Scaffold

## Current scope

P1 Set 025 adds Azure Container Apps Job infrastructure scaffolding for the queue executor.

## Not production-ready yet

Before production use, add:

- container registry publishing
- managed identity role assignments
- Key Vault references
- queue and storage RBAC
- retry/dead-letter implementation
- deployment workflow
- worker health/heartbeat visibility

## Validation path

1. Build worker container.
2. Push to container registry.
3. Run Bicep what-if.
4. Deploy to dev resource group.
5. Run job manually.
6. Confirm queue processing behavior.
