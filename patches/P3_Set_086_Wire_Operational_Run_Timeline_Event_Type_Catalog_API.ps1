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

if ($startup -notmatch "MapOperationalRunTimelineCatalogEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineSearchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineSearchEndpoints\(\);", "api.MapOperationalRunTimelineSearchEndpoints();`r`n        api.MapOperationalRunTimelineCatalogEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineDashboardEndpoints\(\);", "api.MapOperationalRunTimelineDashboardEndpoints();`r`n        api.MapOperationalRunTimelineCatalogEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline catalog endpoint."
}
else {
    Write-Host "Operational run timeline catalog endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineCatalogService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>\(\);", "services.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>();`r`n        services.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);", "services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();`r`n        services.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline catalog service."
}
else {
    Write-Host "Operational run timeline catalog service already registered."
}
