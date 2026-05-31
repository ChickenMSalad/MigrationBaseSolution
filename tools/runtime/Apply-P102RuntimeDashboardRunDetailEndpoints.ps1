[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ProgramPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
if ([string]::IsNullOrWhiteSpace($ProgramPath)) {
    $ProgramPath = 'src\Core\Migration.Admin.Api\Program.cs'
}

$programFullPath = $ProgramPath
if (-not [System.IO.Path]::IsPathRooted($programFullPath)) {
    $programFullPath = Join-Path $repoRoot $ProgramPath
}

if (-not (Test-Path -LiteralPath $programFullPath)) {
    throw ('Program.cs not found: {0}' -f $programFullPath)
}

$text = Get-Content -LiteralPath $programFullPath -Raw
$existingMap = 'app.MapSqlOperationalRuntimeDashboardEndpoints();'
$detailMap = 'app.MapSqlOperationalRuntimeDashboardDetailEndpoints();'

if ($text.IndexOf($detailMap, [System.StringComparison]::Ordinal) -ge 0) {
    Write-Host 'Runtime dashboard detail endpoint mapping already exists.'
    return
}

$existingIndex = $text.IndexOf($existingMap, [System.StringComparison]::Ordinal)
if ($existingIndex -lt 0) {
    throw ('Unable to find required existing dashboard mapping: {0}' -f $existingMap)
}

$insertIndex = $existingIndex + $existingMap.Length
$newText = $text.Substring(0, $insertIndex) + [Environment]::NewLine + $detailMap + $text.Substring($insertIndex)
Set-Content -LiteralPath $programFullPath -Value $newText -Encoding UTF8

Write-Host 'Added runtime dashboard detail endpoint mapping to Admin API Program.cs.'
