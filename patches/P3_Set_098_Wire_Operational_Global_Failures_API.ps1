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

if ($startup -notmatch "MapOperationalGlobalFailureEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityDashboardEndpoints\(\);", "api.MapOperationalGlobalActivityDashboardEndpoints();`r`n        api.MapOperationalGlobalFailureEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalActivityMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityMetricsEndpoints\(\);", "api.MapOperationalGlobalActivityMetricsEndpoints();`r`n        api.MapOperationalGlobalFailureEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational failures endpoint."
}
else {
    Write-Host "Global operational failures endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalFailureService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>\(\);", "services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();`r`n        services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>\(\);", "services.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>();`r`n        services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();"
    }
    else {
        throw "Could not locate operational service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational failure service."
}
else {
    Write-Host "Global operational failure service already registered."
}
