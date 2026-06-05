# P10.2CT - Admin Web Builder Workspace Smoke Harness

Adds a report and optional runtime smoke runner for Manifest, Taxonomy, and Mapping builder routes.

Apply from repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CT\Apply-P10.2CT-AdminWebBuilderWorkspaceSmokeHarness.ps1
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CT\Test-P10.2CT-AdminWebBuilderWorkspaceSmokeHarness.ps1
```

Optional runtime smoke after Admin Web is running:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CT\Run-P10.2CT-AdminWebBuilderWorkspaceSmoke.ps1 -AdminWebBaseUrl "http://localhost:5173"
```
