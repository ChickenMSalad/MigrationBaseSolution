[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $AdminApiAppName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $BaseUrl,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}

if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot 'artifacts\p10'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot 'p10.2b-admin-api-dashboard-cloud-verification.ps1'
}

$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-Path $repoRoot $outputFullPath
}

$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$base = $BaseUrl.TrimEnd('/')
$summaryUrl = '{0}/api/runtime/dashboard/summary' -f $base
$runsUrl = '{0}/api/runtime/dashboard/runs' -f $base

$lines = @(
    '[CmdletBinding()]',
    'param()',
    '',
    'Set-StrictMode -Version 2.0',
    '$ErrorActionPreference = ''Stop''',
    '',
    ('Write-Host ''Checking Admin API App Service settings for {0}...''' -f $AdminApiAppName),
    ('az webapp config appsettings list --resource-group ''{0}'' --name ''{1}'' --query "[?name==''ConnectionStrings__MigrationOperationalStore'' || name==''SqlOperationalRuntimeReadiness__ConnectionString''].name" -o table' -f $ResourceGroup, $AdminApiAppName),
    '',
    'Write-Host ''Checking runtime dashboard summary...''',
    ('Invoke-WebRequest -Uri ''{0}'' -UseBasicParsing | Select-Object StatusCode, StatusDescription, Content' -f $summaryUrl),
    '',
    'Write-Host ''Checking runtime dashboard runs...''',
    ('Invoke-WebRequest -Uri ''{0}'' -UseBasicParsing | Select-Object StatusCode, StatusDescription, Content' -f $runsUrl)
)

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('P10.2B Admin API dashboard cloud verification commands written to {0}' -f $outputFullPath)
