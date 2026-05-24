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

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql'
Assert-PathExists -RootPath $root -RelativePath 'scripts\Test-P7SqlOperationalRuntimeSchema.ps1'
Assert-PathExists -RootPath $root -RelativePath 'docs\p7\P7.8B-SQL-Operational-Runtime-Bootstrap.md'

Write-Host 'P7.8B package validation passed.'
