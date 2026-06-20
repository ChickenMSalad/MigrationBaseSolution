[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

$repo = Resolve-FullPath -Path $RepoRoot
if (-not (Test-Path -LiteralPath $repo)) {
    throw ('RepoRoot does not exist: ' + $repo)
}

$scriptsDir = Join-Path $repo 'scripts'
if (-not (Test-Path -LiteralPath $scriptsDir)) {
    New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null
}

$sourceRoot = $PSScriptRoot
$payloadScripts = @(
    'Invoke-P7OperationalHealthWatch.ps1',
    'Test-P7P1OperationalHealthWatch.ps1'
)

foreach ($scriptName in $payloadScripts) {
    $source = Join-Path $sourceRoot $scriptName
    if (-not (Test-Path -LiteralPath $source)) {
        throw ('Payload script missing: ' + $source)
    }

    $target = Join-Path $scriptsDir $scriptName
    $sourceFull = Resolve-FullPath -Path $source
    $targetFull = Resolve-FullPath -Path $target

    if ($sourceFull -eq $targetFull) {
        Write-Host ('Script already installed: ' + $scriptName)
        continue
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Installed ' + $scriptName)
}

Write-Host 'P7 operational health watch scripts installed. No source files changed.'
