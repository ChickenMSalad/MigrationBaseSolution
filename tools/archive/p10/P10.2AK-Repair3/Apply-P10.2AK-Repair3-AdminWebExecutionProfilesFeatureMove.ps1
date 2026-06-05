Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        if (Test-Path -Path (Join-Path -Path $current -ChildPath 'src/Admin/Migration.Admin.Web') -PathType Container) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    throw 'Unable to locate repo root containing src/Admin/Migration.Admin.Web.'
}

function Ensure-Directory {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-IfNeeded {
    param(
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Destination,
        [Parameter(Mandatory=$true)][string]$Label
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    $destinationDirectory = Split-Path -Path $Destination -Parent
    Ensure-Directory -Path $destinationDirectory
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Set-FileText {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Replace-TextIfPresent {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$OldText,
        [Parameter(Mandatory=$true)][string]$NewText,
        [Parameter(Mandatory=$true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found for {0}: {1}' -f $Label, $Path)
    }

    $content = [System.IO.File]::ReadAllText($Path)
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if ($content.Contains($OldText)) {
        Set-FileText -Path $Path -Text ($content.Replace($OldText, $NewText))
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
        return
    }

    Write-Host ('No update needed for {0}: expected legacy text not present.' -f $Label)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web/src'
$featureRoot = Join-Path -Path $adminSrc -ChildPath 'features/operations/executionProfiles'
$pagePath = Join-Path -Path $featureRoot -ChildPath 'pages/ExecutionProfiles.tsx'
$apiPath = Join-Path -Path $featureRoot -ChildPath 'api/executionProfilesApi.ts'
$typePath = Join-Path -Path $featureRoot -ChildPath 'types/executionProfiles.ts'

Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'pages')
Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'api')
Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'types')

Move-IfNeeded -Source (Join-Path -Path $adminSrc -ChildPath 'pages/ExecutionProfiles.tsx') -Destination $pagePath -Label 'Execution Profiles page'
Move-IfNeeded -Source (Join-Path -Path $adminSrc -ChildPath 'api/executionProfilesApi.ts') -Destination $apiPath -Label 'Execution Profiles API'
Move-IfNeeded -Source (Join-Path -Path $adminSrc -ChildPath 'types/executionProfiles.ts') -Destination $typePath -Label 'Execution Profiles types'

Replace-TextIfPresent -Path $pagePath -OldText '../components/Card' -NewText '../../../../components/Card' -Label 'Execution Profiles page Card import'
Replace-TextIfPresent -Path $pagePath -OldText '../components/LoadingError' -NewText '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Replace-TextIfPresent -Path $pagePath -OldText '../api/executionProfilesApi' -NewText '../api/executionProfilesApi' -Label 'Execution Profiles page API import'
Replace-TextIfPresent -Path $pagePath -OldText '../types/executionProfiles' -NewText '../types/executionProfiles' -Label 'Execution Profiles page types import'

Replace-TextIfPresent -Path $apiPath -OldText './core/adminApiClient' -NewText '../../../../api/core/adminApiClient' -Label 'Execution Profiles API admin client import'
Replace-TextIfPresent -Path $apiPath -OldText '../types/executionProfiles' -NewText '../types/executionProfiles' -Label 'Execution Profiles API types import'

Write-Host 'P10.2AK Repair3 apply completed.'
