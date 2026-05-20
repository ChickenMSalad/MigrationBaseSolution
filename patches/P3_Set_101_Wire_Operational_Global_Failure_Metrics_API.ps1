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

if ($startup -notmatch "MapOperationalGlobalFailureMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalFailureEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalFailureEndpoints\(\);", "api.MapOperationalGlobalFailureEndpoints();`r`n        api.MapOperationalGlobalFailureMetricsEndpoints();"
    }
    else {
        throw "Could not locate global failure endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failure metrics endpoint."
}
else {
    Write-Host "Global operational failure metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureMetricsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>\(\);", "services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();`r`n        services.AddScoped<IOperationalGlobalFailureMetricsService, OperationalGlobalFailureMetricsService>();"
    }
    else {
        throw "Could not locate global failure service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure metrics service."
}
else {
    Write-Host "Global operational failure metrics service already registered."
}
