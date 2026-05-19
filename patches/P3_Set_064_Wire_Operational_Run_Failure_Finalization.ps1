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

if ($startup -notmatch "MapOperationalRunFailureFinalizationEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);", "api.MapOperationalRunCompletionFinalizationEndpoints();`r`n        api.MapOperationalRunFailureFinalizationEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunStatusReconciliationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunStatusReconciliationEndpoints\(\);", "api.MapOperationalRunStatusReconciliationEndpoints();`r`n        api.MapOperationalRunFailureFinalizationEndpoints();"
    }
    else {
        throw "Could not locate operational run endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run failure finalization endpoints."
}
else {
    Write-Host "Operational run failure finalization endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunFailureFinalizationService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>\(\);", "services.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>();`r`n        services.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>();"

    if ($registration -notmatch "IOperationalRunFailureFinalizationService") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>\(\);", "services.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>();`r`n        services.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>();"
    }

    if ($registration -notmatch "IOperationalRunFailureFinalizationService") {
        throw "Could not insert IOperationalRunFailureFinalizationService registration."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run failure finalization service."
}
else {
    Write-Host "Operational run failure finalization service already registered."
}
