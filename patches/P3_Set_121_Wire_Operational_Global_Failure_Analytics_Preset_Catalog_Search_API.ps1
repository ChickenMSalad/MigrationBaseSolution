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

if ($startup -notmatch "MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints();"
    }
    else {
        throw "Could not locate global failure analytics preset endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure analytics preset search endpoint."
}
else {
    Write-Host "Global operational failure analytics preset search endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureAnalyticsPresetSearchService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetDashboardService, OperationalGlobalFailureAnalyticsPresetDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetDashboardService, OperationalGlobalFailureAnalyticsPresetDashboardService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetDashboardService, OperationalGlobalFailureAnalyticsPresetDashboardService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>();"
    }
    else {
        throw "Could not locate analytics preset service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure analytics preset search service."
}
else {
    Write-Host "Global operational failure analytics preset search service already registered."
}
