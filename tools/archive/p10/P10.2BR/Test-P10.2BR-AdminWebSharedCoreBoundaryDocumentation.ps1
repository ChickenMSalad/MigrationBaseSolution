Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')
    return $candidate.Path
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected text was not found in {0}: {1}' -f $Path, $Text)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$apiReadme = Join-Path $adminSrc 'api\README.md'
$typesReadme = Join-Path $adminSrc 'types\README.md'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BR-AdminWebSharedCoreBoundaryDocumentation.Report.md'
$featuresDir = Join-Path $adminSrc 'features'

Assert-FileContains -Path $apiReadme -Text '# Admin Web Shared API Surface'
Assert-FileContains -Path $apiReadme -Text 'Feature-specific API clients should live with their feature'
Assert-FileContains -Path $typesReadme -Text '# Admin Web Shared Types'
Assert-FileContains -Path $typesReadme -Text 'Feature-specific types should live with their feature'
Assert-FileContains -Path $reportPath -Text '# P10.2BR - Admin Web Shared Core Boundary Documentation Report'
Assert-FileContains -Path $reportPath -Text 'Canonical Feature Group Roots'

$expectedFeatureRoots = @('connectors', 'governance', 'operations', 'platform', 'security')
foreach ($featureRoot in $expectedFeatureRoots) {
    $path = Join-Path $featuresDir $featureRoot
    if (-not (Test-Path -Path $path -PathType Container)) {
        throw ('Expected canonical feature root was not found: {0}' -f $path)
    }
}

$appTsx = Join-Path $adminSrc 'App.tsx'
if (Test-Path -Path $appTsx -PathType Leaf) {
    $appText = Get-Content -Path $appTsx -Raw
    if ($appText.IndexOf('.tsx''', [System.StringComparison]::Ordinal) -ge 0 -or $appText.IndexOf('.tsx"', [System.StringComparison]::Ordinal) -ge 0) {
        throw 'App.tsx contains a .tsx import extension.'
    }
}

Write-Host 'P10.2BR Admin Web shared core boundary documentation validation passed.'
