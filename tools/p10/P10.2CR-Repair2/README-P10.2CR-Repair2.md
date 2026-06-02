# P10.2CR Repair2 - Admin Web Builder Promotion Readiness

Report-only repair for P10.2CR. This version avoids PowerShell 5.1 collection-parameter helper patterns and does not mutate Admin Web source.

## Apply

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CR-Repair2\Apply-P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.ps1
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CR-Repair2\Test-P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.ps1
```

## Output

- `docs/P10/P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.md`
