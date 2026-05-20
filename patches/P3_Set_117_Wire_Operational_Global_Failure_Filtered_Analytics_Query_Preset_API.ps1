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

if ($startup -notmatch "MapOperationalGlobalFailureAnalyticsPresetEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureFilteredAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureFilteredAnalyticsEndpoints\(\);", "api.MapOperationalGlobalFailureFilteredAnalyticsEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureAnalyticsDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure analytics preset endpoints."
}
else {
    Write-Host "Global operational failure analytics preset endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureAnalyticsPresetService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>\(\);", "services.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>();"
    }
    else {
        throw "Could not locate filtered analytics service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure analytics preset service."
}
else {
    Write-Host "Global operational failure analytics preset service already registered."
}
