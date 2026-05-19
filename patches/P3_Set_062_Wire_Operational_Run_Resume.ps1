$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

if (-not (Test-Path $registrationPath)) {
    throw "Could not find $registrationPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalRunControlEndpoints\(") {
    if ($startup -match "api\.MapOperationalMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMetricsEndpoints\(\);", "api.MapOperationalMetricsEndpoints();`r`n        api.MapOperationalRunControlEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalWorkItemLeaseExpirationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalWorkItemLeaseExpirationEndpoints\(\);", "api.MapOperationalWorkItemLeaseExpirationEndpoints();`r`n        api.MapOperationalRunControlEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run control endpoints."
}
else {
    Write-Host "Operational run control endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunControlService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalMetricsService, OperationalMetricsService>\(\);", "services.AddScoped<IOperationalMetricsService, OperationalMetricsService>();`r`n        services.AddScoped<IOperationalRunControlService, OperationalRunControlService>();"

    if ($registration -notmatch "IOperationalRunControlService") {
        throw "Could not insert IOperationalRunControlService registration. Add services.AddScoped<IOperationalRunControlService, OperationalRunControlService>(); manually."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run control service."
}
else {
    Write-Host "Operational run control service already registered."
}
