[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$packRoot = Split-Path -Parent $scriptRoot
$payloadRoot = Join-Path $packRoot '_payload'

$relativePath = 'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx'
$source = Join-Path $payloadRoot $relativePath
$target = Join-Path $RepoRoot $relativePath

if (-not (Test-Path -LiteralPath $source)) {
    throw ('Payload source not found: ' + $source)
}
if (-not (Test-Path -LiteralPath $target)) {
    throw ('Target file not found: ' + $target)
}

$backup = $target + '.p7-taxonomy-json-post.bak'
Copy-Item -LiteralPath $target -Destination $backup -Force
Copy-Item -LiteralPath $source -Destination $target -Force

Write-Host 'Installed TaxonomyBuilder.tsx JSON POST repair.'
Write-Host ('Backup: ' + $backup)
