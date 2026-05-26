[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SnapshotPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path (Get-Location).Path $Path)
}

function Get-Names {
    param($Map)
    if ($null -eq $Map) { return @() }
    return @($Map.PSObject.Properties | ForEach-Object { $_.Name } | Sort-Object -Unique)
}

function Add-SettingSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title,
        $Map
    )

    $Lines.Add("## $Title")
    $Lines.Add("")
    $names = @(Get-Names -Map $Map)
    if ($names.Count -eq 0) {
        $Lines.Add("No settings captured.")
        $Lines.Add("")
        return
    }

    foreach ($name in $names) {
        $Lines.Add(('- `{0}`' -f $name))
    }
    $Lines.Add("")
}

$snapshotFullPath = Resolve-FullPath -Path $SnapshotPath
if (-not (Test-Path -LiteralPath $snapshotFullPath -PathType Leaf)) {
    throw "Snapshot file not found: $snapshotFullPath"
}

$snapshot = (Get-Content -LiteralPath $snapshotFullPath -Raw) | ConvertFrom-Json
$outputFullPath = Resolve-FullPath -Path $OutputPath
$outputDirectory = Split-Path -Path $outputFullPath -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add("# Runtime Parity Report")
$lines.Add("")
$lines.Add(('- Environment: {0}' -f $snapshot.environmentName))
$lines.Add(('- Generated UTC: {0}' -f $snapshot.generatedUtc))
$lines.Add("")

Add-SettingSection -Lines $lines -Title "Dispatcher settings" -Map $snapshot.dispatcherAppSettings
Add-SettingSection -Lines $lines -Title "Executor settings" -Map $snapshot.executorAppSettings

$schemaText = [string]$snapshot.sqlSchemaText
$lines.Add("## SQL schema snapshot")
$lines.Add("")
if ([string]::IsNullOrWhiteSpace($schemaText)) {
    $lines.Add("No SQL schema snapshot captured.")
}
else {
    $hasWorkItems = $schemaText.Contains("WorkItems")
    $hasManifestRows = $schemaText.Contains("ManifestRows")
    $hasLegacyWorkItems = $schemaText.Contains("MigrationWorkItems")
    $hasLegacyManifestRecords = $schemaText.Contains("MigrationManifestRecords")
    $lines.Add("- Contains `WorkItems`: $hasWorkItems")
    $lines.Add("- Contains `ManifestRows`: $hasManifestRows")
    $lines.Add("- Contains legacy `MigrationWorkItems`: $hasLegacyWorkItems")
    $lines.Add("- Contains legacy `MigrationManifestRecords`: $hasLegacyManifestRecords")
}
$lines.Add("")

$lines | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
Write-Host "Runtime parity report written to $outputFullPath"
