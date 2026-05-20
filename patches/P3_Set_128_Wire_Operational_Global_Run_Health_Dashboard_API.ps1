$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalGlobalRunHealthDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthSummaryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthSummaryEndpoints\(\);", "api.MapOperationalGlobalRunHealthSummaryEndpoints();`r`n        api.MapOperationalGlobalRunHealthDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);", "api.MapOperationalGlobalActivityDashboardEndpoints();`r`n        api.MapOperationalGlobalRunHealthDashboardEndpoints();"
    }
    else {
        throw "Could not locate operational run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health dashboard endpoint."
}
else {
    Write-Host "Global operational run health dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);", "services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>();"
    }
    else {
        throw "Could not locate operational run health service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health dashboard service."
}
else {
    Write-Host "Global operational run health dashboard service already registered."
}
