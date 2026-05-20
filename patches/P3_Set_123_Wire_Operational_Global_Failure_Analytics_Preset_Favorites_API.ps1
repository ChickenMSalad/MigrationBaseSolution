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

if ($startup -notmatch "MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints();"
    }
    else {
        throw "Could not locate global failure analytics preset endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure analytics preset favorite endpoints."
}
else {
    Write-Host "Global operational failure analytics preset favorite endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureAnalyticsPresetFavoriteService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>\(\);", "services.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>();`r`n        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>();"
    }
    else {
        throw "Could not locate analytics preset service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure analytics preset favorite service."
}
else {
    Write-Host "Global operational failure analytics preset favorite service already registered."
}
