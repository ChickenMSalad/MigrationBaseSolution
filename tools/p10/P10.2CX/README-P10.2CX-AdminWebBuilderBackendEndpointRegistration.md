# P10.2CX - Admin Web Builder Backend Endpoint Registration

Registers existing Admin API builder endpoint classes in the Admin API startup file when the classes are present but not mapped.

## Apply

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CX\Apply-P10.2CX-AdminWebBuilderBackendEndpointRegistration.ps1
powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CX\Test-P10.2CX-AdminWebBuilderBackendEndpointRegistration.ps1
```

## Notes

- Does not create fake endpoints.
- Only registers existing `TaxonomyBuilderEndpoints.cs` and `MappingBuilderEndpoints.cs` if present.
- Writes `docs/P10/P10.2CX-AdminWebBuilderBackendEndpointRegistration.md`.
- Restart Admin API before rerunning runtime smoke checks.
