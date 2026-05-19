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

if ($startup -notmatch "MapOperationalRunTimelineMetricsEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineQueryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineQueryEndpoints\(\);", "api.MapOperationalRunTimelineQueryEndpoints();`r`n        api.MapOperationalRunTimelineMetricsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineEndpoints\(\);", "api.MapOperationalRunTimelineEndpoints();`r`n        api.MapOperationalRunTimelineMetricsEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline metrics endpoint."
}
else {
    Write-Host "Operational run timeline metrics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineMetricsService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>\(\);", "services.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>();`r`n        services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>\(\);", "services.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>();`r`n        services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline metrics service."
}
else {
    Write-Host "Operational run timeline metrics service already registered."
}
