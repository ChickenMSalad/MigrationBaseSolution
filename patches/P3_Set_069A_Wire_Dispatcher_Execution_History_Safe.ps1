$repoRoot = (Resolve-Path ".").Path

$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Missing $startupPath"
}

if (-not (Test-Path $registrationPath)) {
    throw "Missing $registrationPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherExecutionHistoryEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherDiagnosticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherDiagnosticsEndpoints\(\);", "api.MapOperationalDispatcherDiagnosticsEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherEndpoints\(\);", "api.MapOperationalDispatcherEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryEndpoints();"
    }
    else {
        throw "Could not locate dispatcher endpoint mapping insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution history endpoints."
}
else {
    Write-Host "Dispatcher execution history endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IDispatcherExecutionHistoryService") {
    if ($registration -match "services\.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>\(\);", "services.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>\(\);", "services.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();"
    }
    else {
        throw "Could not locate dispatcher service registration insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution history service."
}
else {
    Write-Host "Dispatcher execution history service already registered."
}

Write-Host ""
Write-Host "Safe wiring complete."
Write-Host "Next: apply SQL table script if not already applied:"
Write-Host "src\Migration.Admin.Api\OperationalStore\Sql\Scripts\002_CreateDispatcherExecutions.sql"
