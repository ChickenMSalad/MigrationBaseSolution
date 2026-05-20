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

if ($startup -notmatch "MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureFilteredAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureFilteredAnalyticsEndpoints\(\);", "api.MapOperationalGlobalFailureFilteredAnalyticsEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints();"
    }
    else {
        throw "Could not locate global failure preset endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure analytics preset dashboard endpoint."
}
else {
    Write-Host "Global operational failure analytics preset dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureAnalyticsPresetDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetDashboardService, OperationalGlobalFailureAnalyticsPresetDashboardService>();"
    }
    else {
        throw "Could not locate analytics preset service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure analytics preset dashboard service."
}
else {
    Write-Host "Global operational failure analytics preset dashboard service already registered."
}
