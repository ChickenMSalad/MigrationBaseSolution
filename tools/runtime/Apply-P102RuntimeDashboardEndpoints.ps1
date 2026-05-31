[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot
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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

$programPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Program.cs'
if (-not (Test-Path -LiteralPath $programPath)) {
    throw ('Admin API Program.cs not found: {0}' -f $programPath)
}

$encoding = New-Object System.Text.UTF8Encoding($false)
$programText = [System.IO.File]::ReadAllText($programPath)
if ($null -eq $programText) {
    throw ('Unable to read Admin API Program.cs: {0}' -f $programPath)
}

$usingText = 'using Migration.Admin.Api.Endpoints.Operational.Dashboard;'
$mapCall = 'app.MapSqlOperationalRuntimeDashboardEndpoints();'
$readinessCall = 'app.MapSqlOperationalRuntimeReadinessEndpoints();'
$runCall = 'app.Run();'

$lines = New-Object System.Collections.ArrayList
foreach ($line in ($programText -split "`r?`n")) {
    [void] $lines.Add($line)
}

if ($programText.IndexOf($usingText, [System.StringComparison]::Ordinal) -lt 0) {
    $insertUsingAt = 0
    while ($insertUsingAt -lt $lines.Count -and [string]::IsNullOrWhiteSpace([string] $lines[$insertUsingAt])) {
        $insertUsingAt++
    }
    [void] $lines.Insert($insertUsingAt, $usingText)
}

$programText = [string]::Join([Environment]::NewLine, @($lines))

if ($programText.IndexOf($mapCall, [System.StringComparison]::Ordinal) -lt 0) {
    $lines = New-Object System.Collections.ArrayList
    foreach ($line in ($programText -split "`r?`n")) {
        [void] $lines.Add($line)
    }

    $inserted = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $lineText = [string] $lines[$index]
        if ($lineText.Trim() -eq $readinessCall) {
            [void] $lines.Insert($index + 1, $mapCall)
            $inserted = $true
            break
        }
    }

    if (-not $inserted) {
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $lineText = [string] $lines[$index]
            if ($lineText.Trim() -eq $runCall) {
                [void] $lines.Insert($index, $mapCall)
                $inserted = $true
                break
            }
        }
    }

    if (-not $inserted) {
        throw 'Unable to find a safe insertion point for runtime dashboard endpoint mapping.'
    }

    $programText = [string]::Join([Environment]::NewLine, @($lines))
}

[System.IO.File]::WriteAllText($programPath, $programText, $encoding)
Write-Host 'P10.2A runtime dashboard endpoint mapping applied.'
