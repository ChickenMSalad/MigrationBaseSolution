Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRootSafe {
    if ($PSScriptRoot -and $PSScriptRoot.Length -gt 0) {
        return $PSScriptRoot
    }

    $invocationPath = $MyInvocation.MyCommand.Path
    if ($invocationPath -and $invocationPath.Length -gt 0) {
        return Split-Path -Parent $invocationPath
    }

    return (Get-Location).Path
}

function Find-RepoRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $StartPath
    )

    $current = [System.IO.DirectoryInfo]::new($StartPath)
    while ($null -ne $current) {
        $gitPath = Join-Path -Path $current.FullName -ChildPath '.git'
        $srcPath = Join-Path -Path $current.FullName -ChildPath 'src'
        if ((Test-Path -Path $gitPath) -or (Test-Path -Path $srcPath -PathType Container)) {
            $adminWebPath = Join-Path -Path $current.FullName -ChildPath 'src/Admin/Migration.Admin.Web'
            if (Test-Path -Path $adminWebPath -PathType Container) {
                return $current.FullName
            }
        }

        $current = $current.Parent
    }

    throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = Join-Path -Path $path -ChildPath $segment
    }

    return $path
}

function Move-FileIfNeeded {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Source,

        [Parameter(Mandatory = $true)]
        [string] $Destination
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        if (Test-Path -Path $Source -PathType Leaf) {
            throw "Both source and destination exist. Resolve manually before continuing. Source: $Source Destination: $Destination"
        }

        Write-Host "Already moved: $Destination"
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw "Required source file was not found: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
    }

    Move-Item -Path $Source -Destination $Destination
    Write-Host "Moved: $Source -> $Destination"
}

function Replace-TextInFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $OldText,

        [Parameter(Mandatory = $true)]
        [string] $NewText
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host "Already updated: $Path"
        return
    }

    if (-not $content.Contains($OldText)) {
        throw "Expected text was not found in $Path"
    }

    $updated = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $updated -Encoding UTF8
    Write-Host "Updated: $Path"
}

$scriptRoot = Get-ScriptRootSafe
$repoRoot = Find-RepoRoot -StartPath $scriptRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')

$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'operations', 'executionSessions')
$featurePages = Join-Path -Path $featureRoot -ChildPath 'pages'
$featureApi = Join-Path -Path $featureRoot -ChildPath 'api'
$featureTypes = Join-Path -Path $featureRoot -ChildPath 'types'

foreach ($directory in @($featurePages, $featureApi, $featureTypes)) {
    if (-not (Test-Path -Path $directory -PathType Container)) {
        New-Item -Path $directory -ItemType Directory -Force | Out-Null
    }
}

Move-FileIfNeeded `
    -Source (Join-RepoPath -Root $adminSrc -Segments @('pages', 'ExecutionSessions.tsx')) `
    -Destination (Join-Path -Path $featurePages -ChildPath 'ExecutionSessions.tsx')

Move-FileIfNeeded `
    -Source (Join-RepoPath -Root $adminSrc -Segments @('api', 'executionSessionsApi.ts')) `
    -Destination (Join-Path -Path $featureApi -ChildPath 'executionSessionsApi.ts')

Move-FileIfNeeded `
    -Source (Join-RepoPath -Root $adminSrc -Segments @('types', 'executionSessions.ts')) `
    -Destination (Join-Path -Path $featureTypes -ChildPath 'executionSessions.ts')

$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'
Replace-TextInFile `
    -Path $appPath `
    -OldText 'import { ExecutionSessions } from "./pages/ExecutionSessions";' `
    -NewText 'import { ExecutionSessions } from "./features/operations/executionSessions/pages/ExecutionSessions";'

Write-Host 'P10.2AD Admin Web Execution Sessions feature move applied.'
