Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminApiRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Api'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CX-AdminWebBuilderBackendEndpointRegistration.md'

if (-not (Test-Path -LiteralPath $adminApiRoot -PathType Container)) {
    throw ('Admin API root missing: {0}' -f $adminApiRoot)
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notlike '*P10.2CX - Admin Web Builder Backend Endpoint Registration*') {
    throw 'Report does not contain the expected title.'
}

$startupCandidates = New-Object 'System.Collections.Generic.List[object]'
$csFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.cs' -File | Where-Object {
    $fullName = $_.FullName
    $fullName -notmatch '\\bin\\' -and
    $fullName -notmatch '\\obj\\' -and
    $fullName -notmatch '\\.git\\'
}
foreach ($file in $csFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -like '*app.Map*' -and $content -like '*app.Run*' -and $file.FullName.StartsWith($adminApiRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        [void]$startupCandidates.Add($file)
    }
}

if ($startupCandidates.Count -eq 0) {
    throw 'No Admin API startup candidate found for verification.'
}

$startupPath = $startupCandidates[0].FullName
$startupText = Get-Content -LiteralPath $startupPath -Raw

$taxonomyPath = Join-Path $adminApiRoot 'Endpoints\TaxonomyBuilderEndpoints.cs'
if (Test-Path -LiteralPath $taxonomyPath -PathType Leaf) {
    if ($startupText -notlike '*MapTaxonomyBuilderEndpoints()*') {
        throw 'TaxonomyBuilderEndpoints.cs exists, but startup does not call MapTaxonomyBuilderEndpoints().'
    }
}

$mappingPath = Join-Path $adminApiRoot 'Endpoints\MappingBuilderEndpoints.cs'
if (Test-Path -LiteralPath $mappingPath -PathType Leaf) {
    if ($startupText -notlike '*MapMappingBuilderEndpoints()*') {
        throw 'MappingBuilderEndpoints.cs exists, but startup does not call MapMappingBuilderEndpoints().'
    }
}

if ($startupText -notlike '*using Migration.Admin.Api.Endpoints;*') {
    throw 'Startup file does not import Migration.Admin.Api.Endpoints.'
}

Write-Host 'P10.2CX Admin Web builder backend endpoint registration verification passed.'
