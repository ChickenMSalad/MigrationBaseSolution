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

if ($startup -notmatch "MapOperationalRunTimelineSearchEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineDashboardEndpoints\(\);", "api.MapOperationalRunTimelineDashboardEndpoints();`r`n        api.MapOperationalRunTimelineSearchEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineMetricsEndpoints\(\);", "api.MapOperationalRunTimelineMetricsEndpoints();`r`n        api.MapOperationalRunTimelineSearchEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline search endpoint."
}
else {
    Write-Host "Operational run timeline search endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineSearchService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>\(\);", "services.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>();`r`n        services.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);", "services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();`r`n        services.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline search service."
}
else {
    Write-Host "Operational run timeline search service already registered."
}
