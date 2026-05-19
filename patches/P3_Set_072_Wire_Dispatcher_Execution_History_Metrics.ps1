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

if ($startup -notmatch "MapOperationalDispatcherExecutionMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherExecutionHistoryReadinessEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryReadinessEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryReadinessEndpoints();`r`n        api.MapOperationalDispatcherExecutionMetricsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherExecutionHistoryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryEndpoints();`r`n        api.MapOperationalDispatcherExecutionMetricsEndpoints();"
    }
    else {
        throw "Could not locate dispatcher execution history endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution metrics endpoint."
}
else {
    Write-Host "Dispatcher execution metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IDispatcherExecutionHistoryMetricsService") {
    if ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>();"
    }
    else {
        throw "Could not locate dispatcher execution history service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution metrics service."
}
else {
    Write-Host "Dispatcher execution metrics service already registered."
}
