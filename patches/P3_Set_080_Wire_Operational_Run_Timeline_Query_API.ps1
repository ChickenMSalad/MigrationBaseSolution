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

if ($startup -notmatch "MapOperationalRunTimelineQueryEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunTimelineEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunTimelineEndpoints\(\);", "api.MapOperationalRunTimelineEndpoints();`r`n        api.MapOperationalRunTimelineQueryEndpoints();"
    }
    else {
        throw "Could not locate operational run timeline endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run timeline query endpoint."
}
else {
    Write-Host "Operational run timeline query endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalRunTimelineQueryService") {
    if ($registration -match "services\.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>\(\);", "services.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>();`r`n        services.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>();"
    }
    else {
        throw "Could not locate operational run timeline service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational run timeline query service."
}
else {
    Write-Host "Operational run timeline query service already registered."
}
