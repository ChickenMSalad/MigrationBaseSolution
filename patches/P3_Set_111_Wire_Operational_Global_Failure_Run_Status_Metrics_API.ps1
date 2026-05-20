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

if ($startup -notmatch "MapOperationalGlobalFailureRunStatusMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureSystemPairMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureSystemPairMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureSystemPairMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureRunStatusMetricsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureRunStatusMetricsEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure run-status metrics endpoint."
}
else {
    Write-Host "Global operational failure run-status metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureRunStatusMetricsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>\(\);", "services.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>();`r`n        services.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure run-status metrics service."
}
else {
    Write-Host "Global operational failure run-status metrics service already registered."
}
