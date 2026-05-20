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

if ($startup -notmatch "MapOperationalGlobalActivityFeedEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineGlobalCatalogEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineGlobalCatalogEndpoints\(\);", "api.MapOperationalRunTimelineGlobalCatalogEndpoints();`r`n        api.MapOperationalGlobalActivityFeedEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunTimelineCatalogEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineCatalogEndpoints\(\);", "api.MapOperationalRunTimelineCatalogEndpoints();`r`n        api.MapOperationalGlobalActivityFeedEndpoints();"
    }
    else {
        throw "Could not locate operational timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational activity feed endpoint."
}
else {
    Write-Host "Global operational activity feed endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalActivityFeedService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>\(\);", "services.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>();`r`n        services.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>\(\);", "services.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>();`r`n        services.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>();"
    }
    else {
        throw "Could not locate operational timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational activity feed service."
}
else {
    Write-Host "Global operational activity feed service already registered."
}
