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

if ($startup -notmatch "MapOperationalDispatcherExecutionHistoryReadinessEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherExecutionHistoryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryReadinessEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherDiagnosticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherDiagnosticsEndpoints\(\);", "api.MapOperationalDispatcherDiagnosticsEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryReadinessEndpoints();"
    }
    else {
        throw "Could not locate dispatcher endpoint mapping insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution history readiness endpoint."
}
else {
    Write-Host "Dispatcher execution history readiness endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IDispatcherExecutionHistoryReadinessService") {
    if ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>\(\);", "services.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>();"
    }
    else {
        throw "Could not locate dispatcher service registration insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution history readiness service."
}
else {
    Write-Host "Dispatcher execution history readiness service already registered."
}
