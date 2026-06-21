param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = (Get-Location).Path
}

$payloadRoot = Join-Path (Split-Path -Parent $scriptRoot) '_payload'
if (-not (Test-Path -LiteralPath $payloadRoot)) {
    throw 'Payload folder not found.'
}

$files = @(
    'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs',
    'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Clients\BynderRestClient.cs'
)

foreach ($relativePath in $files) {
    $source = Join-Path $payloadRoot $relativePath
    $target = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ('Payload file not found: ' + $source)
    }

    if (-not (Test-Path -LiteralPath $target)) {
        throw ('Target file not found: ' + $target)
    }

    $backup = $target + '.p7-bynder-taxonomy-connector-refactor.bak'
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Installed ' + $relativePath)
}

Write-Host 'Bynder taxonomy connector refactor installed.'
