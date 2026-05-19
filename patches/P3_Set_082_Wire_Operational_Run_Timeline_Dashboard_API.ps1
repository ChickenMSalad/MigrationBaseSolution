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

if ($startup -notmatch "MapOperationalRunTimelineDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineMetricsEndpoints\(\);", "api.MapOperationalRunTimelineMetricsEndpoints();`r`n        api.MapOperationalRunTimelineDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineQueryEndpoints\(\);", "api.MapOperationalRunTimelineQueryEndpoints();`r`n        api.MapOperationalRunTimelineDashboardEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline dashboard endpoint."
}
else {
    Write-Host "Operational run timeline dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>\(\);", "services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();`r`n        services.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>\(\);", "services.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>();`r`n        services.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline dashboard service."
}
else {
    Write-Host "Operational run timeline dashboard service already registered."
}
