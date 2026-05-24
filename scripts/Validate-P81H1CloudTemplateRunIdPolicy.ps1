Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Assert-PathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-NoActiveRunIdTemplateSetting {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required cloud template missing: $RelativePath"
    }

    $lines = Get-Content -LiteralPath $path
    $lineNumber = 0

    foreach ($line in $lines) {
        $lineNumber++

        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('//') -or
            $trimmed.StartsWith('#') -or
            $trimmed.StartsWith('*')) {
            continue
        }

        if ($trimmed.Contains('MIGRATION_SqlOperationalQueueExecutor__RunId')) {
            throw "Debug-only RunId setting found in active cloud template $RelativePath at line $lineNumber. Remove MIGRATION_SqlOperationalQueueExecutor__RunId from production/cloud templates."
        }
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.1H.1-Remove-Debug-RunId-From-Cloud-Templates.md'

Assert-NoActiveRunIdTemplateSetting `
    -RootPath $root `
    -RelativePath 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json'

Write-Host 'P8.1H.1 cloud template RunId policy validation passed.'
