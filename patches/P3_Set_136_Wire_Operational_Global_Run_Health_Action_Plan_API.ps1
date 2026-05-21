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

if ($startup -notmatch "MapOperationalGlobalRunHealthActionPlanEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthRecommendationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthRecommendationEndpoints\(\);", "api.MapOperationalGlobalRunHealthRecommendationEndpoints();`r`n        api.MapOperationalGlobalRunHealthActionPlanEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthDetailedRiskEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthDetailedRiskEndpoints\(\);", "api.MapOperationalGlobalRunHealthDetailedRiskEndpoints();`r`n        api.MapOperationalGlobalRunHealthActionPlanEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health action plan endpoint."
}
else {
    Write-Host "Global operational run health action plan endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthActionPlanService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthRecommendationService, OperationalGlobalRunHealthRecommendationService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthRecommendationService, OperationalGlobalRunHealthRecommendationService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthRecommendationService, OperationalGlobalRunHealthRecommendationService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthActionPlanService, OperationalGlobalRunHealthActionPlanService>();"
    }
    else {
        throw "Could not locate recommendation service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health action plan service."
}
else {
    Write-Host "Global operational run health action plan service already registered."
}
