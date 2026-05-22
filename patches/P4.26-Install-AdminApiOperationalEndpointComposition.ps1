[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.26] {0}" -f $Message)
}

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target

    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Ensure-Using {
    param(
        [string]$Path,
        [string]$UsingLine
    )

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($UsingLine)) {
        Write-Step ("Already present: {0}" -f $UsingLine)
        return
    }

    if (-not $Apply) {
        Write-Step ("WOULD add using {0}" -f $UsingLine)
        return
    }

    $updated = $UsingLine + [Environment]::NewLine + $content
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8

    Write-Step ("Added using {0}" -f $UsingLine)
}

function Replace-OperationalMappings {
    param([string]$Path)

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains("app.MapMigrationOperationalEndpoints();")) {
        Write-Step "Operational endpoint composition already installed"
        return
    }

    $linesToRemove = @(
        "app.MapSqlOperationalBackboneEndpoints();",
        "app.MapOperationalWorkerTelemetryEndpoints();",
        "app.MapOperationalConnectorConfigurationEndpoints();",
        "app.MapOperationalAuditTrailEndpoints();",
        "app.MapOperationalNotificationEndpoints();",
        "app.MapOperationalSlaSloEndpoints();",
        "app.MapOperationalCapacityEndpoints();",
        "app.MapOperationalCostAnalyticsEndpoints();"
    )

    foreach ($line in $linesToRemove) {
        $content = $content.Replace($line, "")
    }

    $anchor = "app.Run();"

    if (-not $content.Contains($anchor)) {
        throw ("Anchor not found in Program.cs: {0}" -f $anchor)
    }

    $replacement = "app.MapMigrationOperationalEndpoints();" + [Environment]::NewLine + [Environment]::NewLine + $anchor
    $content = $content.Replace($anchor, $replacement)

    if (-not $Apply) {
        Write-Step "WOULD consolidate operational endpoint mappings"
        return
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Consolidated operational endpoint mappings"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Copy-PayloadFile "docs/operations/P4.26-admin-api-operational-endpoint-composition.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"

Ensure-Using `
    -Path $programPath `
    -UsingLine "using Migration.Admin.Api.Endpoints.Operational;"

Replace-OperationalMappings `
    -Path $programPath

Write-Step "Complete."
