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

function Assert-FileExists {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path
    )
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Unexpected legacy file remains for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-ImportLine {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$ExportName,
        [Parameter(Mandatory=$true)][string]$TargetModule
    )
    $content = Read-TextFile -Path $Path
    $name = [System.Text.RegularExpressions.Regex]::Escape($ExportName)
    $module = [System.Text.RegularExpressions.Regex]::Escape($TargetModule)
    $pattern = 'import\s*\{\s*' + $name + '\s*\}\s*from\s*[''\"]' + $module + '[''\"];?'
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
        throw ('Expected import missing for {0}: {1} from {2}' -f $Label, $ExportName, $TargetModule)
    }
}

function Assert-NoBadPowerShellPatterns {
    param([Parameter(Mandatory=$true)][string]$ToolsRoot)

    $scriptFiles = @(Get-ChildItem -Path $ToolsRoot -Filter '*.ps1' -File -Recurse)
    foreach ($script in $scriptFiles) {
        $text = Read-TextFile -Path $script.FullName
        $dollar = [char]36
        $colon = [char]58
        $unsafeColonPatterns = @(
            ($dollar + 'Label' + $colon),
            ($dollar + 'Path' + $colon),
            ($dollar + 'Source' + $colon)
        )
        foreach ($unsafePattern in $unsafeColonPatterns) {
            if ($text.Contains($unsafePattern)) {
                throw ('Unsafe variable-colon interpolation pattern found in {0}' -f $script.FullName)
            }
        }
        $nestedArrayPattern = '@(' + [Environment]::NewLine + '    @(' 
        if ($text.Contains($nestedArrayPattern)) {
            throw ('Unsafe nested validation array pattern found in {0}' -f $script.FullName)
        }
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'operations', 'executionProfiles')
$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ExecutionProfiles.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'executionProfilesApi.ts')
$typesPath = [System.IO.Path]::Combine($featureRoot, 'types', 'executionProfiles.ts')

Assert-FileExists -Label 'Execution Profiles feature page' -Path $pagePath
Assert-FileExists -Label 'Execution Profiles feature API' -Path $apiPath
Assert-FileExists -Label 'Execution Profiles feature types' -Path $typesPath

Assert-FileMissing -Label 'Execution Profiles legacy page' -Path ([System.IO.Path]::Combine($adminSrc, 'pages', 'ExecutionProfiles.tsx'))
Assert-FileMissing -Label 'Execution Profiles legacy API' -Path ([System.IO.Path]::Combine($adminSrc, 'api', 'executionProfilesApi.ts'))
Assert-FileMissing -Label 'Execution Profiles legacy types' -Path ([System.IO.Path]::Combine($adminSrc, 'types', 'executionProfiles.ts'))

Assert-ImportLine -Label 'Execution Profiles page Card import' -Path $pagePath -ExportName 'Card' -TargetModule '../../../../components/Card'
Assert-ImportLine -Label 'Execution Profiles page LoadingError import' -Path $pagePath -ExportName 'LoadingError' -TargetModule '../../../../components/LoadingError'
Assert-ImportLine -Label 'Execution Profiles API client import' -Path $apiPath -ExportName 'adminApiClient' -TargetModule '../../../../api/core/client'

Assert-NoBadPowerShellPatterns -ToolsRoot ([System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AK-Repair4'))

Write-Host 'P10.2AK Repair4 validation passed.'
