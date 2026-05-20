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

if ($startup -notmatch "MapOperationalGlobalFailureAnalyticsDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureRunStatusMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureRunStatusMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureRunStatusMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsDashboardEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure analytics dashboard endpoint."
}
else {
    Write-Host "Global operational failure analytics dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureAnalyticsDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>\(\);", "services.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>\(\);", "services.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure analytics dashboard service."
}
else {
    Write-Host "Global operational failure analytics dashboard service already registered."
}
