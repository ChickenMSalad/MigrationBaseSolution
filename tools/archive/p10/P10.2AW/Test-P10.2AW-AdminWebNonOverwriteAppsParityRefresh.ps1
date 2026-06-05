param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        if (Test-Path -Path $candidate -PathType Container) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Unable to locate repository root from script location.'
}

function Assert-Leaf {
    param([Parameter(Mandatory=$true)] [string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $Path)
    }
}

function Assert-Container {
    param([Parameter(Mandatory=$true)] [string] $Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        throw ('Expected folder was not found: {0}' -f $Path)
    }
}

$repoRoot = Get-RepositoryRoot
$adminSourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appsSourceRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AW-AdminWebNonOverwriteAppsParityRefresh-Report.md')

Assert-Container -Path $adminSourceRoot
Assert-Container -Path $appsSourceRoot
Assert-Leaf -Path $reportPath

$requiredFamilies = @('features', 'components', 'auth', 'lib')
foreach ($family in $requiredFamilies) {
    $appsFamily = [System.IO.Path]::Combine($appsSourceRoot, $family)
    if (Test-Path -Path $appsFamily -PathType Container) {
        $adminFamily = [System.IO.Path]::Combine($adminSourceRoot, $family)
        Assert-Container -Path $adminFamily
    }
}

$reportText = Get-Content -Path $reportPath -Raw
$requiredSections = @(
    '# P10.2AW - Admin Web Non-Overwrite Apps Parity Refresh Report',
    '## Missing apps source copied into canonical Admin Web',
    '## Apps source files already present in canonical Admin Web',
    '## Remaining canonical flat folders'
)
foreach ($section in $requiredSections) {
    if ($reportText.IndexOf($section, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected report section missing: {0}' -f $section)
    }
}

Write-Host 'P10.2AW Admin Web non-overwrite apps parity refresh validation passed.'
