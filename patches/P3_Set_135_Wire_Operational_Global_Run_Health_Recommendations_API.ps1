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

if ($startup -notmatch "MapOperationalGlobalRunHealthRecommendationEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalRunHealthDetailedRiskEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthDetailedRiskEndpoints\(\);", "api.MapOperationalGlobalRunHealthDetailedRiskEndpoints();`r`n        api.MapOperationalGlobalRunHealthRecommendationEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthTrendSummaryEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthTrendSummaryEndpoints\(\);", "api.MapOperationalGlobalRunHealthTrendSummaryEndpoints();`r`n        api.MapOperationalGlobalRunHealthRecommendationEndpoints();"
    }
    else {
        throw "Could not locate run health endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational run health recommendation endpoint."
}
else {
    Write-Host "Global operational run health recommendation endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalRunHealthRecommendationService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalRunHealthDetailedRiskService, OperationalGlobalRunHealthDetailedRiskService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalRunHealthDetailedRiskService, OperationalGlobalRunHealthDetailedRiskService>\(\);", "services.AddScoped<IOperationalGlobalRunHealthDetailedRiskService, OperationalGlobalRunHealthDetailedRiskService>();`r`n        services.AddScoped<IOperationalGlobalRunHealthRecommendationService, OperationalGlobalRunHealthRecommendationService>();"
    }
    else {
        throw "Could not locate detailed risk service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational run health recommendation service."
}
else {
    Write-Host "Global operational run health recommendation service already registered."
}
