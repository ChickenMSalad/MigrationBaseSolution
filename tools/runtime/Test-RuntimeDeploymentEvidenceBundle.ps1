[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $EvidencePath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'artifacts\runtime-deployment-evidence\runtime-deployment-evidence.json'),

    [Parameter(Mandatory = $false)]
    [switch] $AllowMissingOptionalEvidence
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $EvidencePath)) {
    throw ('Evidence bundle file was not found: {0}' -f $EvidencePath)
}

$bundle = Get-Content -LiteralPath $EvidencePath -Raw | ConvertFrom-Json
if ($null -eq $bundle.PSObject.Properties['Evidence']) {
    throw 'Evidence bundle is missing Evidence entries.'
}

$requiredNames = @(
    'runtimeSnapshot',
    'parityReport',
    'sqlRunParentFkValidation',
    'sqlBaselineReconciliationValidation',
    'serviceBusQueueState',
    'workItemState'
)

$evidence = @($bundle.Evidence)
$missing = @()
foreach ($requiredName in $requiredNames) {
    $entry = $evidence | Where-Object { $_.Name -eq $requiredName } | Select-Object -First 1
    if ($null -eq $entry) {
        $missing += ('Missing evidence entry: {0}' -f $requiredName)
        continue
    }

    if (-not [bool] $entry.Exists) {
        $missing += ('Evidence file does not exist: {0}' -f $requiredName)
        continue
    }

    if ([int64] $entry.Length -lt 1) {
        $missing += ('Evidence file is empty: {0}' -f $requiredName)
    }
}

if ($missing.Count -gt 0 -and -not $AllowMissingOptionalEvidence) {
    throw ($missing -join [Environment]::NewLine)
}

Write-Host 'Runtime deployment evidence bundle validation passed.'
