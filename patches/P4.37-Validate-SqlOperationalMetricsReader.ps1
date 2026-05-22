[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/CommandCenter/OperationalCommandCenterEndpointExtensions.cs"
$readerPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/SqlMetrics/SqlOperationalMetricsReader.cs"

Assert-Contains `
    -Path $readerPath `
    -Text "SELECT COUNT(1) FROM dbo.MigrationRuns;"

Assert-Contains `
    -Path $endpointPath `
    -Text "ISqlOperationalMetricsReader metricsReader"

Assert-OccursOnce `
    -Path $programPath `
    -Text "builder.Services.AddScoped<ISqlOperationalMetricsReader, SqlOperationalMetricsReader>();"

Write-Host "[P4.37] Validation passed."
