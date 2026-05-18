# Post-P2 Cleanup Set 009 Fix

## Purpose

Replaces the cleanup checkpoint script with a simpler PowerShell implementation that avoids embedded markdown code-fence strings and fragile quoting.

## Patched file

- `tools/maintenance/validate-post-p2-cleanup.ps1`

## Run

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\validate-post-p2-cleanup.ps1
```
