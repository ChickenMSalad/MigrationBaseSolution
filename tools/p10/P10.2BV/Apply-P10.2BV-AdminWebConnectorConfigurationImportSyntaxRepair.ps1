Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script path.'
        }

        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        if ([System.IO.Directory]::Exists($candidate)) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web.'
        }

        $current = $parent.FullName
    }
}

function Get-ImportClause {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Line
    )

    $fromIndex = $Line.IndexOf(' from ')
    if ($fromIndex -lt 0) {
        return $null
    }

    $prefix = $Line.Substring(0, $fromIndex).Trim()
    if (-not $prefix.StartsWith('import ')) {
        return $null
    }

    $clause = $prefix.Substring(7).Trim()
    if ([string]::IsNullOrWhiteSpace($clause)) {
        return $null
    }

    return $clause
}

function Normalize-ImportLine {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Line
    )

    $trimmed = $Line.Trim()
    if (-not $trimmed.StartsWith('import ')) {
        return $Line
    }

    $clause = Get-ImportClause -Line $trimmed
    if ([string]::IsNullOrWhiteSpace($clause)) {
        return $Line
    }

    if ($trimmed.Contains('connectorConfigurationApi')) {
        return ('import {0} from "../api/connectorConfigurationApi";' -f $clause)
    }

    if ($trimmed.Contains('connectorConfiguration')) {
        return ('import {0} from "../types/connectorConfiguration";' -f $clause)
    }

    if ($trimmed.Contains('components/Card')) {
        return ('import {0} from "../../../../components/Card";' -f $clause)
    }

    if ($trimmed.Contains('components/LoadingError')) {
        return ('import {0} from "../../../../components/LoadingError";' -f $clause)
    }

    if ($trimmed.EndsWith('.tsx";') -or $trimmed.EndsWith('.tsx'';')) {
        return ($trimmed.Replace('.tsx";', '";').Replace('.tsx'';', ''';'))
    }

    return $Line
}

$repoRoot = Get-RepoRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$pagePath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'features', 'connectors', 'configuration', 'pages', 'ConnectorConfiguration.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BV-AdminWebConnectorConfigurationImportSyntaxRepair.Report.md')

if (-not [System.IO.File]::Exists($pagePath)) {
    throw ('Connector Configuration page not found: {0}' -f $pagePath)
}

$lines = [System.IO.File]::ReadAllLines($pagePath)
$updated = New-Object System.Collections.Generic.List[string]
$changes = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    $newLine = Normalize-ImportLine -Line $line
    [void]$updated.Add($newLine)
    if ($newLine -ne $line) {
        [void]$changes.Add(('Updated import: {0} -> {1}' -f $line.Trim(), $newLine.Trim()))
    }
}

if ($changes.Count -gt 0) {
    [System.IO.File]::WriteAllLines($pagePath, $updated.ToArray(), [System.Text.UTF8Encoding]::new($false))
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BV - Admin Web Connector Configuration Import Syntax Repair')
[void]$report.Add('')
[void]$report.Add(('Page: `{0}`' -f $pagePath))
[void]$report.Add('')
[void]$report.Add('## Changes')
if ($changes.Count -eq 0) {
    [void]$report.Add('- No import syntax changes were required.')
} else {
    foreach ($change in $changes) {
        [void]$report.Add(('- {0}' -f $change))
    }
}

$reportDirectory = [System.IO.Path]::GetDirectoryName($reportPath)
if (-not [System.IO.Directory]::Exists($reportDirectory)) {
    [System.IO.Directory]::CreateDirectory($reportDirectory) | Out-Null
}
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.UTF8Encoding]::new($false))

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BV Admin Web Connector Configuration import syntax repair applied.'
