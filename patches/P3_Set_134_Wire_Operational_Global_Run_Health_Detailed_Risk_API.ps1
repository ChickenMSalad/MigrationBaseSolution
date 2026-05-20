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

if ($startup -notmatch "MapOperationalGlobalRunHealthDetailedRiskEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthTrendSummaryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthTrendSummaryEndpoints\(\);", "api.MapOperationalGlobalRunHealthTrendSummaryEndpoints();`r`n        api.MapOperationalGlobalRunHealthDetailedRiskEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthSnapshotEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthSnapshotEndpoints\(\);", "api.MapOperationalGlobalRunHealthSnapshotEndpoints();`r`n        api.MapOperationalGlobalRunHealthDetailedRiskEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health detailed risk endpoint."
}
else {
    Write-Host "Global operational run health detailed risk endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthDetailedRiskService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthTrendSummaryService, OperationalGlobalRunHealthTrendSummaryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthTrendSummaryService, OperationalGlobalRunHealthTrendSummaryService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthTrendSummaryService, OperationalGlobalRunHealthTrendSummaryService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthDetailedRiskService, OperationalGlobalRunHealthDetailedRiskService>();"
    }
    else {
        throw "Could not locate run health trend summary service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health detailed risk service."
}
else {
    Write-Host "Global operational run health detailed risk service already registered."
}
