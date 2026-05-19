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

if ($startup -notmatch "MapOperationalDispatcherDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherExecutionHistoryRetentionEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryRetentionEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryRetentionEndpoints();`r`n        api.MapOperationalDispatcherDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherExecutionHistoryQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryQueryEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryQueryEndpoints();`r`n        api.MapOperationalDispatcherDashboardEndpoints();"
    }
    else {
        throw "Could not locate dispatcher endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational dispatcher dashboard endpoint."
}
else {
    Write-Host "Operational dispatcher dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalDispatcherDashboardSummaryService") {
    if ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>();`r`n        services.AddScoped<IOperationalDispatcherDashboardSummaryService, OperationalDispatcherDashboardSummaryService>();"
    }
    elseif ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>();`r`n        services.AddScoped<IOperationalDispatcherDashboardSummaryService, OperationalDispatcherDashboardSummaryService>();"
    }
    else {
        throw "Could not locate dispatcher service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational dispatcher dashboard service."
}
else {
    Write-Host "Operational dispatcher dashboard service already registered."
}
