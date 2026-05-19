$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherExecutionHistoryQueryEndpoints\(") {

    if ($startup -match "api\.MapOperationalDispatcherExecutionMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionMetricsEndpoints\(\);", "api.MapOperationalDispatcherExecutionMetricsEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryQueryEndpoints();"
    }
    else {
        throw "Could not locate dispatcher metrics mapping."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution query endpoints."
}
else {
    Write-Host "Dispatcher execution query endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IDispatcherExecutionHistoryQueryService") {

    if ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>();"
    }
    else {
        throw "Could not locate dispatcher metrics registration."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution query service."
}
else {
    Write-Host "Dispatcher execution query service already registered."
}
