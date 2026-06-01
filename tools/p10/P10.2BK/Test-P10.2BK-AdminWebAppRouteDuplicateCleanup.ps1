Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }
        $current = $parent.FullName
    }

    throw 'Unable to locate repository root from script location.'
}

$repoRoot = Get-RepoRoot
$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BK-AdminWebAppRouteDuplicateCleanup.Report.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = [System.IO.File]::ReadAllText($appPath)
$lines = @($content -split "`r?`n", -1)

$routeCount = 0
foreach ($line in $lines) {
    if ($line -match '<Route\b') {
        if ($line.Contains('<ConnectorConfiguration') -or $line.Contains('path="/connectors/configuration"') -or $line.Contains("path='/connectors/configuration'")) {
            $routeCount++
        }
    }
}

if ($routeCount -gt 1) {
    throw ('Expected at most one ConnectorConfiguration route, found {0}.' -f $routeCount)
}

$importOperationalEvents = $false
$usesOperationalEvents = $false
foreach ($line in $lines) {
    if ($line -match '^\s*import\s+\{\s*OperationalEvents\s*\}\s+from\s+') {
        $importOperationalEvents = $true
    } elseif ($line.Contains('<OperationalEvents')) {
        $usesOperationalEvents = $true
    }
}

if ($importOperationalEvents -and -not $usesOperationalEvents) {
    throw 'OperationalEvents import is present but no <OperationalEvents usage was found.'
}

if ($content.Contains('ConnectorConfiguration""')) {
    throw 'Malformed ConnectorConfiguration double quote token is still present.'
}

Write-Host 'P10.2BK Admin Web App route duplicate cleanup validation passed.'
