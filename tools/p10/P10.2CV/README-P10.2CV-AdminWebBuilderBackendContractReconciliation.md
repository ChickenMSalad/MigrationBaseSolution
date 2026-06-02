# P10.2CV - Admin Web Builder Backend Contract Reconciliation

This set reconciles restored builder pages against local backend route definitions without guessing or adding fake endpoints.

## Apply

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CV\Apply-P10.2CV-AdminWebBuilderBackendContractReconciliation.ps1
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CV\Test-P10.2CV-AdminWebBuilderBackendContractReconciliation.ps1
```

## Outputs

- `docs/P10/P10.2CV-AdminWebBuilderBackendContractReconciliation.md`
- `artifacts/p10/P10.2CV/builder-backend-contract-reconciliation.details.csv`
