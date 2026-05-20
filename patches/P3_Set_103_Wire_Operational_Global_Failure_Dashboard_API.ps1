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

if ($startup -notmatch "MapOperationalGlobalFailureDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureEndpoints\(\);", "api.MapOperationalGlobalFailureEndpoints();`r`n        api.MapOperationalGlobalFailureDashboardEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure dashboard endpoint."
}
else {
    Write-Host "Global operational failure dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureMetricsService, OperationalGlobalFailureMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureMetricsService, OperationalGlobalFailureMetricsService>\(\);", "services.AddScoped<IOperationalGlobalFailureMetricsService, OperationalGlobalFailureMetricsService>();`r`n        services.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure dashboard service."
}
else {
    Write-Host "Global operational failure dashboard service already registered."
}
