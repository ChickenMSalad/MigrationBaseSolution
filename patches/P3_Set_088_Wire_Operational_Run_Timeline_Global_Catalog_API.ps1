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

if ($startup -notmatch "MapOperationalRunTimelineGlobalCatalogEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineCatalogEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineCatalogEndpoints\(\);", "api.MapOperationalRunTimelineCatalogEndpoints();`r`n        api.MapOperationalRunTimelineGlobalCatalogEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineSearchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineSearchEndpoints\(\);", "api.MapOperationalRunTimelineSearchEndpoints();`r`n        api.MapOperationalRunTimelineGlobalCatalogEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline global catalog endpoint."
}
else {
    Write-Host "Operational run timeline global catalog endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineGlobalCatalogService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>\(\);", "services.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>();`r`n        services.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>\(\);", "services.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>();`r`n        services.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline global catalog service."
}
else {
    Write-Host "Operational run timeline global catalog service already registered."
}
