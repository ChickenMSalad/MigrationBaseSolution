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

if ($startup -notmatch "MapOperationalGlobalRunHealthSnapshotEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthDashboardEndpoints\(\);", "api.MapOperationalGlobalRunHealthDashboardEndpoints();`r`n        api.MapOperationalGlobalRunHealthSnapshotEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthSummaryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthSummaryEndpoints\(\);", "api.MapOperationalGlobalRunHealthSummaryEndpoints();`r`n        api.MapOperationalGlobalRunHealthSnapshotEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health snapshot endpoint."
}
else {
    Write-Host "Global operational run health snapshot endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthSnapshotService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>();"
    }
    else {
        throw "Could not locate run health service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health snapshot service."
}
else {
    Write-Host "Global operational run health snapshot service already registered."
}
