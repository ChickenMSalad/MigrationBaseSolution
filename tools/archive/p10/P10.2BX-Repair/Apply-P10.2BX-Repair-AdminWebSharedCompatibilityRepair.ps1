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
$adminWebRoot = Join-Path $repoRoot 'src/Admin/Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$pagePath = Join-Path $sourceRoot 'features/operations/operationalEvents/pages/OperationalEvents.tsx'
$cardPath = Join-Path $sourceRoot 'components/Card.tsx'
$loadingErrorPath = Join-Path $sourceRoot 'components/LoadingError.tsx'
$reportPath = Join-Path $repoRoot 'docs/P10/P10.2BX-Repair-AdminWebSharedCompatibilityRepair.md'

if (-not (Test-Path -Path $pagePath -PathType Leaf)) {
    throw ('OperationalEvents page was not found: {0}' -f $pagePath)
}
if (-not (Test-Path -Path $cardPath -PathType Leaf)) {
    throw ('Canonical Card component was not found: {0}' -f $cardPath)
}
if (-not (Test-Path -Path $loadingErrorPath -PathType Leaf)) {
    throw ('Canonical LoadingError component was not found: {0}' -f $loadingErrorPath)
}

$content = Get-Content -Path $pagePath -Raw
$original = $content

$content = $content.Replace("from '../components/Card'", "from '../../../../components/Card'")
$content = $content.Replace('from "../components/Card"', 'from "../../../../components/Card"')
$content = $content.Replace("from '../components/LoadingError'", "from '../../../../components/LoadingError'")
$content = $content.Replace('from "../components/LoadingError"', 'from "../../../../components/LoadingError"')

if ($content -ne $original) {
    Set-Content -Path $pagePath -Value $content -Encoding UTF8
    Write-Host ('Updated OperationalEvents component imports: {0}' -f $pagePath)
} else {
    Write-Host ('No OperationalEvents component import updates were needed: {0}' -f $pagePath)
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BX Repair - Admin Web Shared Compatibility Repair')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Normalized OperationalEvents component imports only.')
[void]$report.Add('- No route, package, or feature move changes.')
[void]$report.Add('')
[void]$report.Add('## Files')
[void]$report.Add('')
[void]$report.Add(('- Page: `{0}`' -f $pagePath))
[void]$report.Add(('- Card: `{0}`' -f $cardPath))
[void]$report.Add(('- LoadingError: `{0}`' -f $loadingErrorPath))

$reportDirectory = Split-Path -Path $reportPath -Parent
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BX Repair applied.'
