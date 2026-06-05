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
$docsPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsPath, 'P10.2BK-AdminWebAppRouteDuplicateCleanup.Report.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsPath -PathType Container)) {
    New-Item -ItemType Directory -Path $docsPath -Force | Out-Null
}

$content = [System.IO.File]::ReadAllText($appPath)
$lineBreak = "`n"
if ($content.Contains("`r`n")) {
    $lineBreak = "`r`n"
}

$lines = @($content -split "`r?`n", -1)
if ($lines.Length -gt 0 -and $lines[$lines.Length - 1] -eq '') {
    $hasTrailingNewline = $true
    $sourceLines = @($lines[0..($lines.Length - 2)])
} else {
    $hasTrailingNewline = $false
    $sourceLines = @($lines)
}

$bodyLines = New-Object System.Collections.Generic.List[string]
foreach ($line in $sourceLines) {
    if ($line -notmatch '^\s*import\s+') {
        [void]$bodyLines.Add($line)
    }
}

$bodyText = [string]::Join($lineBreak, $bodyLines.ToArray())
$operationalEventsIsUsed = $bodyText.Contains('<OperationalEvents')

$outputLines = New-Object System.Collections.Generic.List[string]
$removedOperationalEventsImports = 0
$connectorConfigurationRouteCount = 0
$removedConnectorConfigurationRoutes = 0

foreach ($line in $sourceLines) {
    $isOperationalEventsImport = ($line -match '^\s*import\s+\{\s*OperationalEvents\s*\}\s+from\s+')
    if ($isOperationalEventsImport -and -not $operationalEventsIsUsed) {
        $removedOperationalEventsImports++
        continue
    }

    $isConnectorConfigurationRoute = $false
    if ($line -match '<Route\b') {
        if ($line.Contains('<ConnectorConfiguration') -or $line.Contains('path="/connectors/configuration"') -or $line.Contains("path='/connectors/configuration'")) {
            $isConnectorConfigurationRoute = $true
        }
    }

    if ($isConnectorConfigurationRoute) {
        $connectorConfigurationRouteCount++
        if ($connectorConfigurationRouteCount -gt 1) {
            $removedConnectorConfigurationRoutes++
            continue
        }
    }

    [void]$outputLines.Add($line)
}

$newContent = [string]::Join($lineBreak, $outputLines.ToArray())
if ($hasTrailingNewline) {
    $newContent = $newContent + $lineBreak
}

if ($newContent -ne $content) {
    [System.IO.File]::WriteAllText($appPath, $newContent, [System.Text.UTF8Encoding]::new($false))
    Write-Host ('Updated App.tsx: {0}' -f $appPath)
} else {
    Write-Host ('No App.tsx changes were needed: {0}' -f $appPath)
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BK - Admin Web App Route Duplicate Cleanup Report')
[void]$report.Add('')
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add('')
[void]$report.Add('## Actions')
[void]$report.Add('')
[void]$report.Add(('- OperationalEvents used outside imports: `{0}`' -f $operationalEventsIsUsed))
[void]$report.Add(('- OperationalEvents import lines removed: `{0}`' -f $removedOperationalEventsImports))
[void]$report.Add(('- ConnectorConfiguration route lines seen: `{0}`' -f $connectorConfigurationRouteCount))
[void]$report.Add(('- Duplicate ConnectorConfiguration route lines removed: `{0}`' -f $removedConnectorConfigurationRoutes))
[void]$report.Add('')
[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- This set is intentionally narrow.')
[void]$report.Add('- It does not move files.')
[void]$report.Add('- It does not touch `/apps`.')
[void]$report.Add('- It preserves the first ConnectorConfiguration route found in local App.tsx.')

[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.UTF8Encoding]::new($false))
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BK Admin Web App route duplicate cleanup applied.'
