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

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not $pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull
    }

    $relative = $pathFull.Substring($rootFull.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    return ($relative -replace '\\', '/')
}

function Get-FilesOrEmpty {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$Include = @('*')
    )

    if (-not (Test-Path -Path $Path -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -Path $Path -File -Recurse -Include $Include | Sort-Object -Property FullName)
}

function Get-DirectoriesOrEmpty {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -Path $Path -Directory -Recurse | Sort-Object -Property FullName)
}

function Write-SectionList {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Builder,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][object[]]$Items
    )

    [void]$Builder.AppendLine('## ' + $Title)
    [void]$Builder.AppendLine('')
    if ($Items.Count -eq 0) {
        [void]$Builder.AppendLine('- None found')
        [void]$Builder.AppendLine('')
        return
    }

    foreach ($item in $Items) {
        [void]$Builder.AppendLine('- `' + [string]$item + '`')
    }
    [void]$Builder.AppendLine('')
}

function Get-TopFeatureFamilies {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$FeatureRoot
    )

    if (-not (Test-Path -Path $FeatureRoot -PathType Container)) {
        return @()
    }

    $families = New-Object System.Collections.Generic.List[string]
    $firstLevel = @(Get-ChildItem -Path $FeatureRoot -Directory | Sort-Object -Property Name)
    foreach ($family in $firstLevel) {
        $children = @(Get-ChildItem -Path $family.FullName -Directory -ErrorAction SilentlyContinue | Sort-Object -Property Name)
        if ($children.Count -eq 0) {
            $families.Add((Get-RelativePath -Root $Root -Path $family.FullName))
            continue
        }

        foreach ($child in $children) {
            $families.Add((Get-RelativePath -Root $Root -Path $child.FullName))
        }
    }

    return @($families.ToArray())
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('src', 'Admin', 'Migration.Admin.Web', 'src'))
$appsSrc = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('apps', 'migration-admin-ui', 'src'))
$reportPath = Join-Path -Path $repoRoot -ChildPath ([System.IO.Path]::Combine('docs', 'P10', 'P10.2AP-AdminWebAppsParityInventoryAndBatchPlan.md'))

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminSrc)
}

if (-not (Test-Path -Path $appsSrc -PathType Container)) {
    throw ('Reference apps Admin UI source root was not found: {0}' -f $appsSrc)
}

$adminFeatures = Join-Path -Path $adminSrc -ChildPath 'features'
$appsFeatures = Join-Path -Path $appsSrc -ChildPath 'features'
$adminPages = Join-Path -Path $adminSrc -ChildPath 'pages'
$adminApi = Join-Path -Path $adminSrc -ChildPath 'api'
$adminTypes = Join-Path -Path $adminSrc -ChildPath 'types'
$adminComponents = Join-Path -Path $adminSrc -ChildPath 'components'
$appsComponents = Join-Path -Path $appsSrc -ChildPath 'components'

$adminFeatureFamilies = @(Get-TopFeatureFamilies -Root $repoRoot -FeatureRoot $adminFeatures)
$appsFeatureFamilies = @(Get-TopFeatureFamilies -Root $repoRoot -FeatureRoot $appsFeatures)
$flatPages = @(Get-FilesOrEmpty -Path $adminPages -Include @('*.tsx') | ForEach-Object { Get-RelativePath -Root $repoRoot -Path $_.FullName })
$flatApi = @(Get-FilesOrEmpty -Path $adminApi -Include @('*.ts') | Where-Object { $_.FullName -notlike ('*' + [System.IO.Path]::DirectorySeparatorChar + 'core' + [System.IO.Path]::DirectorySeparatorChar + '*') } | ForEach-Object { Get-RelativePath -Root $repoRoot -Path $_.FullName })
$flatTypes = @(Get-FilesOrEmpty -Path $adminTypes -Include @('*.ts') | ForEach-Object { Get-RelativePath -Root $repoRoot -Path $_.FullName })
$adminComponentFiles = @(Get-FilesOrEmpty -Path $adminComponents -Include @('*.tsx', '*.ts', '*.css') | ForEach-Object { Get-RelativePath -Root $repoRoot -Path $_.FullName })
$appsComponentFiles = @(Get-FilesOrEmpty -Path $appsComponents -Include @('*.tsx', '*.ts', '*.css') | ForEach-Object { Get-RelativePath -Root $repoRoot -Path $_.FullName })

$batchCandidates = New-Object System.Collections.Generic.List[object]
$knownFamilies = @(
    [pscustomobject]@{ Name = 'Connector and credential screens'; PageHints = @('Connector', 'Credential', 'Credentials'); Target = 'features/connectors and features/security' },
    [pscustomobject]@{ Name = 'Runtime and operations screens'; PageHints = @('Run', 'Runtime', 'Execution', 'Failure', 'Operational', 'Command'); Target = 'features/operations' },
    [pscustomobject]@{ Name = 'Governance and audit screens'; PageHints = @('Audit', 'Notification', 'Policy'); Target = 'features/governance' },
    [pscustomobject]@{ Name = 'Platform analytics screens'; PageHints = @('Capacity', 'Cost', 'Telemetry'); Target = 'features/platform' },
    [pscustomobject]@{ Name = 'Project authoring screens'; PageHints = @('Manifest', 'Mapping', 'Taxonomy', 'Project', 'Preflight'); Target = 'features/workspace or features/projects' },
    [pscustomobject]@{ Name = 'Artifact and storage screens'; PageHints = @('Artifact', 'Storage'); Target = 'features/platform/artifacts' }
)

foreach ($family in $knownFamilies) {
    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($page in $flatPages) {
        $fileName = [System.IO.Path]::GetFileNameWithoutExtension($page)
        foreach ($hint in $family.PageHints) {
            if ($fileName.IndexOf($hint, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $matches.Add($page)
                break
            }
        }
    }

    if ($matches.Count -gt 0) {
        $batchCandidates.Add([pscustomobject]@{
            Name = $family.Name
            Target = $family.Target
            Pages = @($matches.ToArray())
        })
    }
}

$builder = New-Object System.Text.StringBuilder
[void]$builder.AppendLine('# P10.2AP - Admin Web Apps Parity Inventory And Batch Plan')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('Generated from the local repository tree. Failed partial repair runs do not need to be committed before this report is regenerated.')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('## Purpose')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('Inventory canonical Admin Web and `/apps/migration-admin-ui` so remaining UI consolidation can be bundled by feature family instead of moved one file at a time.')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('## Roots')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('- Canonical Admin Web: `' + (Get-RelativePath -Root $repoRoot -Path $adminSrc) + '`')
[void]$builder.AppendLine('- Reference apps UI: `' + (Get-RelativePath -Root $repoRoot -Path $appsSrc) + '`')
[void]$builder.AppendLine('')

Write-SectionList -Builder $builder -Title 'Canonical Admin Web feature families' -Items $adminFeatureFamilies
Write-SectionList -Builder $builder -Title 'Reference apps feature families' -Items $appsFeatureFamilies
Write-SectionList -Builder $builder -Title 'Remaining canonical flat pages' -Items $flatPages
Write-SectionList -Builder $builder -Title 'Remaining canonical flat API files excluding api/core' -Items $flatApi
Write-SectionList -Builder $builder -Title 'Remaining canonical flat type files' -Items $flatTypes
Write-SectionList -Builder $builder -Title 'Canonical component files' -Items $adminComponentFiles
Write-SectionList -Builder $builder -Title 'Reference apps component files' -Items $appsComponentFiles

[void]$builder.AppendLine('## Recommended batch plan')
[void]$builder.AppendLine('')
if ($batchCandidates.Count -eq 0) {
    [void]$builder.AppendLine('- No page-family batch candidates were detected from the remaining flat pages.')
} else {
    $batchNumber = 1
    foreach ($candidate in $batchCandidates) {
        [void]$builder.AppendLine(('{0}. {1} -> `{2}`' -f $batchNumber, $candidate.Name, $candidate.Target))
        foreach ($page in $candidate.Pages) {
            [void]$builder.AppendLine('   - `' + $page + '`')
        }
        $batchNumber++
    }
}
[void]$builder.AppendLine('')
[void]$builder.AppendLine('## Guardrails for next implementation package')
[void]$builder.AppendLine('')
[void]$builder.AppendLine('- Validate all source and destination paths before moving files.')
[void]$builder.AppendLine('- Do not require stale exact import strings; normalize import declarations by source path only.')
[void]$builder.AppendLine('- Do not treat a failed apply or failed repair as committed state.')
[void]$builder.AppendLine('- Do not scan the active tool folder for literal validator safety strings.')
[void]$builder.AppendLine('- Keep `/apps/migration-admin-ui` as reference source only until the canonical Admin Web has absorbed required features/components.')

$reportDirectory = Split-Path -Path $reportPath -Parent
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}

Set-Content -Path $reportPath -Value $builder.ToString() -Encoding UTF8
Write-Host ('Wrote Admin Web apps parity inventory report: {0}' -f $reportPath)
