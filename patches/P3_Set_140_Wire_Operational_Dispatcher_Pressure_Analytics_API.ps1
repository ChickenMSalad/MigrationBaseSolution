$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherPressureAnalyticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);", "api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();`r`n        api.MapOperationalDispatcherPressureAnalyticsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherDashboardEndpoints\(\);", "api.MapOperationalDispatcherDashboardEndpoints();`r`n        api.MapOperationalDispatcherPressureAnalyticsEndpoints();"
    }
    else {
        throw "Could not locate dispatcher endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational dispatcher pressure analytics endpoint."
}
else {
    Write-Host "Operational dispatcher pressure analytics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalDispatcherPressureAnalyticsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalQueueDepthAnalyticsService, OperationalGlobalQueueDepthAnalyticsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalQueueDepthAnalyticsService, OperationalGlobalQueueDepthAnalyticsService>\(\);", "services.AddScoped<IOperationalGlobalQueueDepthAnalyticsService, OperationalGlobalQueueDepthAnalyticsService>();`r`n        services.AddScoped<IOperationalDispatcherPressureAnalyticsService, OperationalDispatcherPressureAnalyticsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IDispatcherExecutionMetricsService, DispatcherExecutionMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionMetricsService, DispatcherExecutionMetricsService>\(\);", "services.AddScoped<IDispatcherExecutionMetricsService, DispatcherExecutionMetricsService>();`r`n        services.AddScoped<IOperationalDispatcherPressureAnalyticsService, OperationalDispatcherPressureAnalyticsService>();"
    }
    else {
        throw "Could not locate dispatcher/queue service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational dispatcher pressure analytics service."
}
else {
    Write-Host "Operational dispatcher pressure analytics service already registered."
}
