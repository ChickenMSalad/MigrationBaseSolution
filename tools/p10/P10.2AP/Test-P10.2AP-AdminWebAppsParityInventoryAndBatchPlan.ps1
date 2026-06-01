Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -Path $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.Path
        }

        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or ($parent -eq $current.Path)) {
            break
        }

        $current = Resolve-Path -Path $parent
    }

    throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $Path)
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected text missing for {0}: {1}' -f $Label, $Text)
    }
}

function Assert-NoUnsafePowerShellPatterns {
    param([Parameter(Mandatory = $true)][string]$ToolRoot)

    $scripts = @(Get-ChildItem -Path $ToolRoot -File -Filter '*.ps1' | Sort-Object -Property FullName)
    foreach ($script in $scripts) {
        $content = Get-Content -Path $script.FullName -Raw
        if ($content -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
            throw ('Unsafe variable-colon interpolation pattern found in {0}' -f $script.FullName)
        }
        if ($content -match '@\(\s*@\(') {
            throw ('Unsafe nested array validation pattern found in {0}' -f $script.FullName)
        }
        if ($content -match 'src\t' -or $content -match 'src\a') {
            throw ('Potential corrupted escape sequence pattern found in {0}' -f $script.FullName)
        }
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('src', 'Admin', 'Migration.Admin.Web', 'src'))
$appsSrc = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('apps', 'migration-admin-ui', 'src'))
$reportPath = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('docs', 'P10', 'P10.2AP-AdminWebAppsParityInventoryAndBatchPlan.md'))
$toolRoot = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('tools', 'p10', 'P10.2AP'))

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminSrc)
}
if (-not (Test-Path -Path $appsSrc -PathType Container)) {
    throw ('Reference apps Admin UI source root was not found: {0}' -f $appsSrc)
}

Assert-FileExists -Path $reportPath
Assert-TextContains -Path $reportPath -Text '# P10.2AP - Admin Web Apps Parity Inventory And Batch Plan' -Label 'report title'
Assert-TextContains -Path $reportPath -Text 'Remaining canonical flat pages' -Label 'flat pages section'
Assert-TextContains -Path $reportPath -Text 'Reference apps feature families' -Label 'apps feature section'
Assert-TextContains -Path $reportPath -Text 'Recommended batch plan' -Label 'batch plan section'
Assert-NoUnsafePowerShellPatterns -ToolRoot $toolRoot

Write-Host 'P10.2AP Admin Web apps parity inventory and batch plan validation passed.'
