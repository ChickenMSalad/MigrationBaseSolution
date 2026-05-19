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

if ($startup -notmatch "MapOperationalRunTimelineEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunDashboardEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunDashboardEndpoints\(\);", "api.MapOperationalRunDashboardEndpoints();`r`n        api.MapOperationalRunTimelineEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunStatusProjectionEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunStatusProjectionEndpoints\(\);", "api.MapOperationalRunStatusProjectionEndpoints();`r`n        api.MapOperationalRunTimelineEndpoints();"
    }
    else {
        throw "Could not locate operational run endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline endpoint."
}
else {
    Write-Host "Operational run timeline endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineService") {
    if ($registration -match "services\.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>\(\);", "services.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>();`r`n        services.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunStatusProjectionService, OperationalRunStatusProjectionService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunStatusProjectionService, OperationalRunStatusProjectionService>\(\);", "services.AddScoped<IOperationalRunStatusProjectionService, OperationalRunStatusProjectionService>();`r`n        services.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>();"
    }
    else {
        throw "Could not locate operational run service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline service."
}
else {
    Write-Host "Operational run timeline service already registered."
}
