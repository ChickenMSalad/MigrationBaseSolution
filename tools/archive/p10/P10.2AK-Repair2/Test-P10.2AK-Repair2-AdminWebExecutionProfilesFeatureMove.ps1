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

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path,[Parameter(Mandatory=$true)][string]$Label)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileAbsent {
    param([Parameter(Mandatory=$true)][string]$Path,[Parameter(Mandatory=$true)][string]$Label)

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$Label
    )

    Assert-FileExists -Path $Path -Label $Label
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text missing for {0}: {1}' -f $Label, $Text)
    }
}

function Test-PathHasSegment {
    param([Parameter(Mandatory=$true)][string]$Path,[Parameter(Mandatory=$true)][string]$Segment)

    $parts = $Path -split [System.Text.RegularExpressions.Regex]::Escape([System.IO.Path]::DirectorySeparatorChar)
    if ([System.IO.Path]::AltDirectorySeparatorChar -ne [System.IO.Path]::DirectorySeparatorChar) {
        $expanded = New-Object System.Collections.Generic.List[string]
        foreach ($part in $parts) {
            foreach ($inner in ($part -split [System.Text.RegularExpressions.Regex]::Escape([System.IO.Path]::AltDirectorySeparatorChar))) {
                if ($inner.Length -gt 0) { [void]$expanded.Add($inner) }
            }
        }
        $parts = $expanded.ToArray()
    }

    return ($parts -contains $Segment)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-PathSafe -Parts @($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-PathSafe -Parts @($adminSrc, 'features', 'operations', 'executionProfiles')

$pagePath = Join-PathSafe -Parts @($featureRoot, 'pages', 'ExecutionProfiles.tsx')
$apiPath = Join-PathSafe -Parts @($featureRoot, 'api', 'executionProfilesApi.ts')
$typePath = Join-PathSafe -Parts @($featureRoot, 'types', 'executionProfiles.ts')
$appPath = Join-PathSafe -Parts @($adminSrc, 'App.tsx')

Assert-FileExists -Path $pagePath -Label 'Execution Profiles feature page'
Assert-FileExists -Path $apiPath -Label 'Execution Profiles feature API'
Assert-FileExists -Path $typePath -Label 'Execution Profiles feature types'
Assert-FileExists -Path $appPath -Label 'App.tsx'

Assert-FileAbsent -Path (Join-PathSafe -Parts @($adminSrc, 'pages', 'ExecutionProfiles.tsx')) -Label 'flat Execution Profiles page'
Assert-FileAbsent -Path (Join-PathSafe -Parts @($adminSrc, 'api', 'executionProfilesApi.ts')) -Label 'flat Execution Profiles API'
Assert-FileAbsent -Path (Join-PathSafe -Parts @($adminSrc, 'types', 'executionProfiles.ts')) -Label 'flat Execution Profiles types'

Assert-Contains -Path $pagePath -Text '../../../../components/Card' -Label 'Execution Profiles Card import'
Assert-Contains -Path $pagePath -Text '../../../../components/LoadingError' -Label 'Execution Profiles LoadingError import'
Assert-Contains -Path $apiPath -Text '../../../../api/core/client' -Label 'Execution Profiles API client import'

$appContent = Get-Content -Path $appPath -Raw
if ($appContent -match 'ExecutionProfiles') {
    Assert-Contains -Path $appPath -Text './features/operations/executionProfiles/pages/ExecutionProfiles' -Label 'Execution Profiles App import when referenced'
}
else {
    Write-Host 'App.tsx does not reference ExecutionProfiles; feature move is valid without an App import.'
}

$scriptRoot = Split-Path -Parent $PSCommandPath
$toolRoot = Join-PathSafe -Parts @($repoRoot, 'tools', 'p10')
$scripts = @(Get-ChildItem -Path $toolRoot -Filter '*.ps1' -Recurse | Where-Object {
    -not (Test-PathHasSegment -Path $_.FullName -Segment 'bin') -and
    -not (Test-PathHasSegment -Path $_.FullName -Segment 'obj') -and
    -not ($_.FullName.StartsWith($scriptRoot, [System.StringComparison]::OrdinalIgnoreCase))
})

foreach ($script in $scripts) {
    $content = Get-Content -Path $script.FullName -Raw
    if ($content.Contains('$Label:') -or $content.Contains('$Path:')) {
        throw ('Unsafe colon interpolation pattern found in {0}' -f $script.FullName)
    }
}

Write-Host 'P10.2AK Repair2 validation passed.'
