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

if ($startup -notmatch "MapOperationalGlobalRunHealthSummaryEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints();`r`n        api.MapOperationalGlobalRunHealthSummaryEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);", "api.MapOperationalGlobalActivityDashboardEndpoints();`r`n        api.MapOperationalGlobalRunHealthSummaryEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health summary endpoint."
}
else {
    Write-Host "Global operational run health summary endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthSummaryService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);", "services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>();"
    }
    else {
        throw "Could not locate operational service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health summary service."
}
else {
    Write-Host "Global operational run health summary service already registered."
}
