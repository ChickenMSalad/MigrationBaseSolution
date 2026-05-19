$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw
if ($startup -notmatch "MapOperationalRunStatusReconciliationEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunControlEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunControlEndpoints\(\);", "api.MapOperationalRunControlEndpoints();`r`n        api.MapOperationalRunStatusReconciliationEndpoints();"
    } elseif ($startup -match "api\.MapOperationalMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMetricsEndpoints\(\);", "api.MapOperationalMetricsEndpoints();`r`n        api.MapOperationalRunStatusReconciliationEndpoints();"
    } else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }
    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run status reconciliation endpoints."
}

$registration = Get-Content $registrationPath -Raw
if ($registration -notmatch "IOperationalRunStatusReconciliationService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalRunControlService, OperationalRunControlService>\(\);", "services.AddScoped<IOperationalRunControlService, OperationalRunControlService>();`r`n        services.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>();"
    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run status reconciliation service."
}
