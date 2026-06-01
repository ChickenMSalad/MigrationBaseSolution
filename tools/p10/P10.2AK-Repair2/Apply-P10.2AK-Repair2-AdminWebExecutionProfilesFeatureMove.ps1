Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Split-Path -Parent $PSCommandPath
    while ($null -ne $current -and $current.Length -gt 0) {
        if (Test-Path -Path (Join-Path $current 'src') -PathType Container) {
            if (Test-Path -Path (Join-Path $current 'Directory.Build.props') -PathType Leaf) {
                return $current
            }
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) { break }
        $current = $parent
    }

    throw 'Unable to locate repository root from script path.'
}

function Join-PathSafe {
    param([Parameter(Mandatory=$true)][string[]]$Parts)

    $result = $Parts[0]
    for ($i = 1; $i -lt $Parts.Count; $i++) {
        $result = Join-Path $result $Parts[$i]
    }

    return $result
}

function Require-File {
    param([Parameter(Mandatory=$true)][string]$Path,[Parameter(Mandatory=$true)][string]$Label)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file missing for {0}: {1}' -f $Label, $Path)
    }
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
        throw ('Neither source nor destination exists for {0}. Source: {1} Destination: {2}' -f $Label, $Source, $Destination)
    }

    Ensure-Directory -Path (Split-Path -Parent $Destination)
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-TextIfPresent {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$OldText,
        [Parameter(Mandatory=$true)][string]$NewText,
        [Parameter(Mandatory=$true)][string]$Label
    )

    Require-File -Path $Path -Label $Label
    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if ($content.Contains($OldText)) {
        $updated = $content.Replace($OldText, $NewText)
        Set-Content -Path $Path -Value $updated -NoNewline
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
        return
    }

    Write-Host ('No update needed for {0}: expected legacy text not present.' -f $Label)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-PathSafe -Parts @($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-PathSafe -Parts @($adminSrc, 'features', 'operations', 'executionProfiles')

$pageSource = Join-PathSafe -Parts @($adminSrc, 'pages', 'ExecutionProfiles.tsx')
$apiSource = Join-PathSafe -Parts @($adminSrc, 'api', 'executionProfilesApi.ts')
$typeSource = Join-PathSafe -Parts @($adminSrc, 'types', 'executionProfiles.ts')

$pageDestination = Join-PathSafe -Parts @($featureRoot, 'pages', 'ExecutionProfiles.tsx')
$apiDestination = Join-PathSafe -Parts @($featureRoot, 'api', 'executionProfilesApi.ts')
$typeDestination = Join-PathSafe -Parts @($featureRoot, 'types', 'executionProfiles.ts')
$appPath = Join-PathSafe -Parts @($adminSrc, 'App.tsx')

Move-IfNeeded -Source $pageSource -Destination $pageDestination -Label 'Execution Profiles page'
Move-IfNeeded -Source $apiSource -Destination $apiDestination -Label 'Execution Profiles API'
Move-IfNeeded -Source $typeSource -Destination $typeDestination -Label 'Execution Profiles types'

Replace-TextIfPresent -Path $pageDestination -OldText '../components/Card' -NewText '../../../../components/Card' -Label 'Execution Profiles page Card import'
Replace-TextIfPresent -Path $pageDestination -OldText '../components/LoadingError' -NewText '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Replace-TextIfPresent -Path $pageDestination -OldText '../api/executionProfilesApi' -NewText '../api/executionProfilesApi' -Label 'Execution Profiles page API import no-op check'
Replace-TextIfPresent -Path $pageDestination -OldText '../types/executionProfiles' -NewText '../types/executionProfiles' -Label 'Execution Profiles page type import no-op check'
Replace-TextIfPresent -Path $apiDestination -OldText './core/client' -NewText '../../../../api/core/client' -Label 'Execution Profiles API core client import'
Replace-TextIfPresent -Path $apiDestination -OldText '../types/executionProfiles' -NewText '../types/executionProfiles' -Label 'Execution Profiles API type import no-op check'

Require-File -Path $appPath -Label 'App.tsx'
$appContent = Get-Content -Path $appPath -Raw
if ($appContent -match 'ExecutionProfiles') {
    $legacyImportPattern = 'import\s+\{\s*ExecutionProfiles\s*\}\s+from\s+["'']\.\/pages\/ExecutionProfiles["''];'
    $featureImport = 'import { ExecutionProfiles } from "./features/operations/executionProfiles/pages/ExecutionProfiles";'
    if ($appContent.Contains($featureImport)) {
        Write-Host 'App.tsx already references the Execution Profiles feature import.'
    }
    elseif ($appContent -match $legacyImportPattern) {
        $updatedApp = [System.Text.RegularExpressions.Regex]::Replace($appContent, $legacyImportPattern, $featureImport, [System.Text.RegularExpressions.RegexOptions]::None)
        Set-Content -Path $appPath -Value $updatedApp -NoNewline
        Write-Host 'Updated App.tsx Execution Profiles import.'
    }
    else {
        throw 'App.tsx references ExecutionProfiles, but no supported import shape was found.'
    }
}
else {
    Write-Host 'App.tsx does not reference ExecutionProfiles; no App.tsx import is required.'
}

Write-Host 'P10.2AK Repair2 apply completed.'
