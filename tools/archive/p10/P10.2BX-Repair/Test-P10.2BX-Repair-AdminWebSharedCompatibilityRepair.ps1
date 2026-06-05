Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if ((Test-Path -Path (Join-Path $current 'src/Admin/Migration.Admin.Web') -PathType Container) -and
            (Test-Path -Path (Join-Path $current 'tools') -PathType Container)) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }

    if ($PSScriptRoot) {
        $candidate = (Resolve-Path -Path (Join-Path $PSScriptRoot '../../..')).Path
        if (Test-Path -Path (Join-Path $candidate 'src/Admin/Migration.Admin.Web') -PathType Container) {
            return $candidate
        }
    }

    throw 'Unable to resolve repository root. Run from the repository root or from a checked-out repository path.'
}

$repoRoot = Get-RepoRoot
$sourceRoot = Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src'
$pagePath = Join-Path $sourceRoot 'features/operations/operationalEvents/pages/OperationalEvents.tsx'
$reportPath = Join-Path $repoRoot 'docs/P10/P10.2BX-Repair-AdminWebSharedCompatibilityRepair.md'

if (-not (Test-Path -Path $pagePath -PathType Leaf)) {
    throw ('OperationalEvents page was not found: {0}' -f $pagePath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = Get-Content -Path $pagePath -Raw
if ($content.Contains("from '../components/Card'")) {
    throw 'OperationalEvents still imports Card from ../components.'
}
if ($content.Contains('from "../components/Card"')) {
    throw 'OperationalEvents still imports Card from ../components.'
}
if ($content.Contains("from '../components/LoadingError'")) {
    throw 'OperationalEvents still imports LoadingError from ../components.'
}
if ($content.Contains('from "../components/LoadingError"')) {
    throw 'OperationalEvents still imports LoadingError from ../components.'
}
if (-not ($content.Contains("from '../../../../components/Card'") -or $content.Contains('from "../../../../components/Card"'))) {
    throw 'OperationalEvents does not import Card from canonical components.'
}
if (-not ($content.Contains("from '../../../../components/LoadingError'") -or $content.Contains('from "../../../../components/LoadingError"'))) {
    throw 'OperationalEvents does not import LoadingError from canonical components.'
}
if ($content.Contains('.tsx''') -or $content.Contains('.tsx"')) {
    throw 'OperationalEvents contains an extension-bearing TSX import.'
}

Write-Host 'P10.2BX Repair validation passed.'
