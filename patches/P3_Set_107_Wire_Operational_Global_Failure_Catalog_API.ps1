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

if ($startup -notmatch "MapOperationalGlobalFailureCatalogEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureQueryEndpoints\(\);", "api.MapOperationalGlobalFailureQueryEndpoints();`r`n        api.MapOperationalGlobalFailureCatalogEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureDashboardEndpoints\(\);", "api.MapOperationalGlobalFailureDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureCatalogEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure catalog endpoint."
}
else {
    Write-Host "Global operational failure catalog endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureCatalogService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>\(\);", "services.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>();`r`n        services.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure catalog service."
}
else {
    Write-Host "Global operational failure catalog service already registered."
}
