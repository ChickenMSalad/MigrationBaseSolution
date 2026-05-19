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

if ($startup -notmatch "MapOperationalDispatcherExecutionHistoryRetentionEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherExecutionHistoryQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionHistoryQueryEndpoints\(\);", "api.MapOperationalDispatcherExecutionHistoryQueryEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryRetentionEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatcherExecutionMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherExecutionMetricsEndpoints\(\);", "api.MapOperationalDispatcherExecutionMetricsEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryRetentionEndpoints();"
    }
    else {
        throw "Could not locate dispatcher execution endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution history retention endpoints."
}
else {
    Write-Host "Dispatcher execution history retention endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "DispatcherExecutionHistoryRetentionOptions") {
    if ($registration -match "services\.Configure<OperationalDispatcherOptions>") {
        $registration = $registration -replace "(services\.Configure<OperationalDispatcherOptions>\(\s*configuration\.GetSection\(OperationalDispatcherOptions\.SectionName\)\);)", "`$1`r`n`r`n        services.Configure<DispatcherExecutionHistoryRetentionOptions>(`r`n            configuration.GetSection(DispatcherExecutionHistoryRetentionOptions.SectionName));"
    }
    else {
        throw "Could not locate dispatcher options registration."
    }
}

if ($registration -notmatch "IDispatcherExecutionHistoryRetentionService") {
    if ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>();"
    }
    elseif ($registration -match "services\.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>\(\);", "services.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>();"
    }
    else {
        throw "Could not locate dispatcher execution history service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution history retention options/service."
}
else {
    Write-Host "Dispatcher execution history retention service already registered."
}
