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

if ($startup -notmatch "MapOperationalGlobalRunHealthOperationsCenterEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);", "api.MapOperationalGlobalRunHealthActionPlanEndpoints();`r`n        api.MapOperationalGlobalRunHealthOperationsCenterEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthRecommendationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthRecommendationEndpoints\(\);", "api.MapOperationalGlobalRunHealthRecommendationEndpoints();`r`n        api.MapOperationalGlobalRunHealthOperationsCenterEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health operations center endpoint."
}
else {
    Write-Host "Global operational run health operations center endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthOperationsCenterService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthOperationsCenterService, OperationalGlobalRunHealthOperationsCenterService>();"
    }
    else {
        throw "Could not locate run health action plan service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health operations center service."
}
else {
    Write-Host "Global operational run health operations center service already registered."
}
