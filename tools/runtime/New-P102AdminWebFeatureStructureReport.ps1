[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = 'artifacts/admin-ui-consolidation/p10-admin-web-feature-structure-report.md'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param(
        [Parameter(Mandatory = $false)]
        [string] $Candidate
    )

    if (-not [string]::IsNullOrWhiteSpace($Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
            $scriptRoot = Split-Path -Parent $PSCommandPath
        }
    }

    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        throw 'Unable to resolve script root.'
    }

    return (Split-Path -Parent (Split-Path -Parent $scriptRoot))
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $current = $Root
    foreach ($part in ($RelativePath -split '/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $current = [System.IO.Path]::Combine($current, $part)
        }
    }
    return $current
}

function Test-ExcludedPathBySegment {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FullName
    )

    $segments = $FullName.Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    foreach ($segment in $segments) {
        if ($segment -eq 'node_modules' -or
            $segment -eq 'dist' -or
            $segment -eq 'build' -or
            $segment -eq '.git' -or
            $segment -eq '.vite') {
            return $true
        }
    }
    return $false
}

function Get-RelativeFileList {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $basePath = Join-RepoPath -Root $Root -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $basePath -PathType Container)) {
        return @()
    }

    $files = Get-ChildItem -LiteralPath $basePath -Recurse -File | Where-Object {
        -not (Test-ExcludedPathBySegment -FullName $_.FullName)
    }

    return @($files | ForEach-Object {
        $_.FullName.Substring($Root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    } | Sort-Object)
}

$resolvedRepoRoot = Resolve-RepoRoot -Candidate $RepoRoot
$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-RepoPath -Root $resolvedRepoRoot -RelativePath $OutputPath
}

$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent -PathType Container)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$sections = @(
    [pscustomobject]@{ Title = 'Canonical pages'; Path = 'src/Admin/Migration.Admin.Web/src/pages' },
    [pscustomobject]@{ Title = 'Canonical API clients'; Path = 'src/Admin/Migration.Admin.Web/src/api' },
    [pscustomobject]@{ Title = 'Canonical types'; Path = 'src/Admin/Migration.Admin.Web/src/types' },
    [pscustomobject]@{ Title = 'Canonical components'; Path = 'src/Admin/Migration.Admin.Web/src/components' },
    [pscustomobject]@{ Title = 'Canonical feature folders'; Path = 'src/Admin/Migration.Admin.Web/src/features' },
    [pscustomobject]@{ Title = 'Feature-source folders'; Path = 'apps/migration-admin-ui/src/features' },
    [pscustomobject]@{ Title = 'Feature-source components'; Path = 'apps/migration-admin-ui/src/components' }
)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P10.2Z Admin Web Feature Structure Report') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('- Generated UTC: {0}' -f ([DateTimeOffset]::UtcNow.ToString('o')))) | Out-Null
$lines.Add(('- Repository root: {0}' -f $resolvedRepoRoot)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Canonical UI: `src/Admin/Migration.Admin.Web`') | Out-Null
$lines.Add('Feature-source UI: `apps/migration-admin-ui`') | Out-Null
$lines.Add('') | Out-Null

foreach ($section in $sections) {
    $items = @(Get-RelativeFileList -Root $resolvedRepoRoot -RelativePath $section.Path)
    $lines.Add(('## {0}' -f $section.Title)) | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add(('- Path: `{0}`' -f $section.Path)) | Out-Null
    $lines.Add(('- Files: {0}' -f $items.Count)) | Out-Null
    $lines.Add('') | Out-Null
    foreach ($item in $items) {
        $lines.Add(('- `{0}`' -f ($item -replace '\\','/'))) | Out-Null
    }
    $lines.Add('') | Out-Null
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('P10.2Z Admin Web feature structure report written to {0}' -f $outputFullPath)
