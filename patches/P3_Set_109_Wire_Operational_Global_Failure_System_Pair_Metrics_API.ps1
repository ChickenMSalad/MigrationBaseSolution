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

if ($startup -notmatch "MapOperationalGlobalFailureSystemPairMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureCatalogEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureCatalogEndpoints\(\);", "api.MapOperationalGlobalFailureCatalogEndpoints();`r`n        api.MapOperationalGlobalFailureSystemPairMetricsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureMetricsEndpoints\(\);", "api.MapOperationalGlobalFailureMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureSystemPairMetricsEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure system-pair metrics endpoint."
}
else {
    Write-Host "Global operational failure system-pair metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureSystemPairMetricsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>\(\);", "services.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>();`r`n        services.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure system-pair metrics service."
}
else {
    Write-Host "Global operational failure system-pair metrics service already registered."
}
