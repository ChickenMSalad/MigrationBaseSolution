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

if ($startup -notmatch "MapOperationalGlobalFailureQueryEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureQueryEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureQueryEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure query endpoint."
}
else {
    Write-Host "Global operational failure query endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureQueryService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>\(\);", "services.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>();`r`n        services.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure query service."
}
else {
    Write-Host "Global operational failure query service already registered."
}
