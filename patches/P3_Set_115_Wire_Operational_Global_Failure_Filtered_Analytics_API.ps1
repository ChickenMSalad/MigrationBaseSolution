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

if ($startup -notmatch "MapOperationalGlobalFailureFilteredAnalyticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureAnalyticsDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureFilteredAnalyticsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureQueryEndpoints\(\);", "api.MapOperationalGlobalFailureQueryEndpoints();`r`n        api.MapOperationalGlobalFailureFilteredAnalyticsEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure filtered analytics endpoint."
}
else {
    Write-Host "Global operational failure filtered analytics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureFilteredAnalyticsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>();`r`n        services.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>\(\);", "services.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>();`r`n        services.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure filtered analytics service."
}
else {
    Write-Host "Global operational failure filtered analytics service already registered."
}
