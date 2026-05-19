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

if ($startup -notmatch "MapOperationalRunDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);", "api.MapOperationalRunCompletionFinalizationEndpoints();`r`n        api.MapOperationalRunDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunStatusProjectionEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunStatusProjectionEndpoints\(\);", "api.MapOperationalRunStatusProjectionEndpoints();`r`n        api.MapOperationalRunDashboardEndpoints();"
    }
    else {
        throw "Could not locate operational run endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run dashboard endpoint."
}
else {
    Write-Host "Operational run dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunDashboardSummaryService") {
    if ($registration -match "services\.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>\(\);", "services.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>();`r`n        services.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>\(\);", "services.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>();`r`n        services.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>();"
    }
    else {
        throw "Could not locate operational run service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run dashboard service."
}
else {
    Write-Host "Operational run dashboard service already registered."
}
