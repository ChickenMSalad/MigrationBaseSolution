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

if ($startup -notmatch "MapOperationalGlobalActivityMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalActivityQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityQueryEndpoints\(\);", "api.MapOperationalGlobalActivityQueryEndpoints();`r`n        api.MapOperationalGlobalActivityMetricsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalActivityFeedEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityFeedEndpoints\(\);", "api.MapOperationalGlobalActivityFeedEndpoints();`r`n        api.MapOperationalGlobalActivityMetricsEndpoints();"
    }
    else {
        throw "Could not locate global activity endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational activity metrics endpoint."
}
else {
    Write-Host "Global operational activity metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalActivityMetricsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>\(\);", "services.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>();`r`n        services.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>\(\);", "services.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>();`r`n        services.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>();"
    }
    else {
        throw "Could not locate global activity service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational activity metrics service."
}
else {
    Write-Host "Global operational activity metrics service already registered."
}
