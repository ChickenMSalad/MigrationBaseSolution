[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file not found: $Path"
    }
}

function Assert-DirectoryExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Expected directory not found: $Path"
    }
}

function Assert-TextExists {
    param([string]$Path, [string]$Text)
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected text not found in {0}: {1}' -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$uiRoot = Join-Path $repoRoot 'apps\migration-admin-ui'

Assert-DirectoryExists $uiRoot
Assert-FileExists (Join-Path $uiRoot 'package.json')
Assert-FileExists (Join-Path $uiRoot 'src\App.tsx')
Assert-FileExists (Join-Path $uiRoot 'src\lib\adminApi.ts')
Assert-FileExists (Join-Path $repoRoot 'docs\ui\P4.11-operational-ui-shell.md')

Assert-TextExists (Join-Path $uiRoot 'src\App.tsx') '/api/system/endpoints'
Assert-TextExists (Join-Path $uiRoot 'src\App.tsx') '/api/operational/runtime-readiness/queue'
Assert-TextExists (Join-Path $uiRoot 'src\lib\adminApi.ts') 'VITE_ADMIN_API_BASE_URL'
Assert-TextExists (Join-Path $uiRoot 'package.json') '"build": "tsc -b && vite build"'

Write-Host '[P4.11] Validation passed.'
