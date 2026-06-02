# P10.2CY - Admin Web Builder Backend Verb-Aware Smoke

Apply:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CY\Apply-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CY\Test-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1
```

Optional runner after Admin API is running:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CY\Run-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1 -AdminApiBaseUrl "https://localhost:55436"
```
