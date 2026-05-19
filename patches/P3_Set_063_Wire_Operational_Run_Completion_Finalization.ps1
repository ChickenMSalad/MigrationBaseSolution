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

if ($startup -notmatch "MapOperationalRunCompletionFinalizationEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunStatusReconciliationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunStatusReconciliationEndpoints\(\);", "api.MapOperationalRunStatusReconciliationEndpoints();`r`n        api.MapOperationalRunCompletionFinalizationEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunControlEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunControlEndpoints\(\);", "api.MapOperationalRunControlEndpoints();`r`n        api.MapOperationalRunCompletionFinalizationEndpoints();"
    }
    else {
        throw "Could not locate operational run endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run completion finalization endpoints."
}
else {
    Write-Host "Operational run completion finalization endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunCompletionFinalizationService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>\(\);", "services.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>();`r`n        services.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>();"

    if ($registration -notmatch "IOperationalRunCompletionFinalizationService") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunControlService, OperationalRunControlService>\(\);", "services.AddScoped<IOperationalRunControlService, OperationalRunControlService>();`r`n        services.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>();"
    }

    if ($registration -notmatch "IOperationalRunCompletionFinalizationService") {
        throw "Could not insert IOperationalRunCompletionFinalizationService registration."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run completion finalization service."
}
else {
    Write-Host "Operational run completion finalization service already registered."
}
