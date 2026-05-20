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

if ($startup -notmatch "MapOperationalGlobalActivityDashboardEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalActivityMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityMetricsEndpoints\(\);", "api.MapOperationalGlobalActivityMetricsEndpoints();`r`n        api.MapOperationalGlobalActivityDashboardEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalActivityQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityQueryEndpoints\(\);", "api.MapOperationalGlobalActivityQueryEndpoints();`r`n        api.MapOperationalGlobalActivityDashboardEndpoints();"
    }
    else {
        throw "Could not locate global activity endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational activity dashboard endpoint."
}
else {
    Write-Host "Global operational activity dashboard endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalActivityDashboardService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>\(\);", "services.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>();`r`n        services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>\(\);", "services.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>();`r`n        services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();"
    }
    else {
        throw "Could not locate global activity service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational activity dashboard service."
}
else {
    Write-Host "Global operational activity dashboard service already registered."
}
