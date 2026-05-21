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

if ($startup -notmatch "MapOperationalGlobalQueueDepthAnalyticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthOperationsCenterEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthOperationsCenterEndpoints\(\);", "api.MapOperationalGlobalRunHealthOperationsCenterEndpoints();`r`n        api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);", "api.MapOperationalGlobalRunHealthActionPlanEndpoints();`r`n        api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational queue depth analytics endpoint."
}
else {
    Write-Host "Global operational queue depth analytics endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalQueueDepthAnalyticsService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthOperationsCenterService, OperationalGlobalRunHealthOperationsCenterService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthOperationsCenterService, OperationalGlobalRunHealthOperationsCenterService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthOperationsCenterService, OperationalGlobalRunHealthOperationsCenterService>();`r`n        services.AddScoped<IOperationalGlobalQueueDepthAnalyticsService, OperationalGlobalQueueDepthAnalyticsService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>();`r`n        services.AddScoped<IOperationalGlobalQueueDepthAnalyticsService, OperationalGlobalQueueDepthAnalyticsService>();"
    }
    else {
        throw "Could not locate operational service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational queue depth analytics service."
}
else {
    Write-Host "Global operational queue depth analytics service already registered."
}
