Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = [System.IO.Path]::Combine($current.Path, 'src', 'Admin', 'Migration.Admin.Web', 'src')
        if (Test-Path -Path $candidate -PathType Container) {
            return $current.Path
        }
        $current = $current.Parent
    }
    throw 'Unable to locate repository root from current directory.'
}

function Read-TextFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Replace-ImportLine {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$ExportName,
        [Parameter(Mandatory=$true)][string]$TargetModule,
        [Parameter(Mandatory=$true)][string]$Label
    )

    $content = Read-TextFile -Path $Path
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ExportName)
    $pattern = 'import\s*\{\s*' + $escapedName + '\s*\}\s*from\s*[''\"][^''\"]+[''\"];?'
    $replacement = 'import { ' + $ExportName + ' } from "' + $TargetModule + '";'
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

    if ($updated -eq $content) {
        if ($content.Contains($replacement)) {
            Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
            return
        }
        throw ('Unable to update {0}; import for {1} was not found in {2}' -f $Label, $ExportName, $Path)
    }

    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'operations', 'executionProfiles')
$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ExecutionProfiles.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'executionProfilesApi.ts')
$typesPath = [System.IO.Path]::Combine($featureRoot, 'types', 'executionProfiles.ts')

$requiredFiles = @(
    [pscustomobject]@{ Label = 'Execution Profiles page'; Path = $pagePath },
    [pscustomobject]@{ Label = 'Execution Profiles API'; Path = $apiPath },
    [pscustomobject]@{ Label = 'Execution Profiles types'; Path = $typesPath }
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -Path $file.Path -PathType Leaf)) {
        throw ('Required moved file was not found for {0}: {1}' -f $file.Label, $file.Path)
    }
}

Replace-ImportLine -Path $pagePath -ExportName 'Card' -TargetModule '../../../../components/Card' -Label 'Execution Profiles page Card import'
Replace-ImportLine -Path $pagePath -ExportName 'LoadingError' -TargetModule '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Replace-ImportLine -Path $apiPath -ExportName 'adminApiClient' -TargetModule '../../../../api/core/client' -Label 'Execution Profiles API client import'

Write-Host 'P10.2AK Repair4 apply completed.'
