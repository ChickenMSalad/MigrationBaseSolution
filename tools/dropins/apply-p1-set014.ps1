$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set014-operational-health"

Write-Host "Applying P1 Set 014 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Contracts\OperationalHealthContracts.cs",
    "src\Migration.Admin.Api\Endpoints\OperationalHealthEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\operationalHealth.ts",
    "docs\cloud-roadmap-cleanup\P1_SET_014_OPERATIONAL_HEALTH.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "MapOperationalHealthEndpoints") {
    if ($program -match "app\.MapAdminSystemEndpoints\(\);") {
        $program = $program -replace "app\.MapAdminSystemEndpoints\(\);", "app.MapAdminSystemEndpoints();`r`napp.MapOperationalHealthEndpoints();"
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs after app.MapAdminSystemEndpoints();"
    }
    elseif ($program -match "var api = app\.MapGroup\(") {
        $program = $program -replace "var api = app\.MapGroup\(", "app.MapOperationalHealthEndpoints();`r`n`r`nvar api = app.MapGroup("
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs before API group creation."
    }
    else {
        throw "Could not find Program.cs health endpoint mapping anchor. No partial patch was written."
    }
}
else {
    Write-Host "Program.cs already maps operational health endpoints."
}

Write-Host ""
Write-Host "P1 Set 014 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/health/live"
Write-Host "  Invoke-RestMethod http://localhost:5173/health/ready"
Write-Host "  Invoke-RestMethod http://localhost:5173/health/cloud"
