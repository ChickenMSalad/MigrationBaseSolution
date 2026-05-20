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

if ($startup -notmatch "MapOperationalGlobalRunHealthTrendSummaryEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthSnapshotEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthSnapshotEndpoints\(\);", "api.MapOperationalGlobalRunHealthSnapshotEndpoints();`r`n        api.MapOperationalGlobalRunHealthTrendSummaryEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthDashboardEndpoints\(\);", "api.MapOperationalGlobalRunHealthDashboardEndpoints();`r`n        api.MapOperationalGlobalRunHealthTrendSummaryEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health trend summary endpoint."
}
else {
    Write-Host "Global operational run health trend summary endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthTrendSummaryService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthTrendSummaryService, OperationalGlobalRunHealthTrendSummaryService>();"
    }
    else {
        throw "Could not locate run health snapshot service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health trend summary service."
}
else {
    Write-Host "Global operational run health trend summary service already registered."
}
