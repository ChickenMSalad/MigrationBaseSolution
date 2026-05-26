[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BaselineSnapshotPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CandidateSnapshotPath,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path (Get-Location).Path $Path)
}

function Read-Snapshot {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = Resolve-FullPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Snapshot file not found: $fullPath"
    }

    return (Get-Content -LiteralPath $fullPath -Raw) | ConvertFrom-Json
}

function Get-MapValue {
    param($Map, [string]$Name)

    if ($null -eq $Map) { return $null }
    $property = $Map.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-Names {
    param($Map)

    if ($null -eq $Map) { return @() }
    return @($Map.PSObject.Properties | ForEach-Object { $_.Name } | Sort-Object -Unique)
}

function Add-SettingDiffs {
    param(
        [System.Collections.ArrayList]$Diffs,
        [string]$Scope,
        $BaselineMap,
        $CandidateMap
    )

    $names = @((Get-Names -Map $BaselineMap) + (Get-Names -Map $CandidateMap) | Sort-Object -Unique)
    foreach ($name in $names) {
        $baselineValue = Get-MapValue -Map $BaselineMap -Name $name
        $candidateValue = Get-MapValue -Map $CandidateMap -Name $name

        if ($null -eq $baselineValue -and $null -ne $candidateValue) {
            [void]$Diffs.Add([ordered]@{ severity = "info"; scope = $Scope; name = $name; kind = "added" })
            continue
        }

        if ($null -ne $baselineValue -and $null -eq $candidateValue) {
            [void]$Diffs.Add([ordered]@{ severity = "warning"; scope = $Scope; name = $name; kind = "removed" })
            continue
        }

        if ([string]$baselineValue -ne [string]$candidateValue) {
            [void]$Diffs.Add([ordered]@{ severity = "warning"; scope = $Scope; name = $name; kind = "changed" })
        }
    }
}

$baseline = Read-Snapshot -Path $BaselineSnapshotPath
$candidate = Read-Snapshot -Path $CandidateSnapshotPath
$diffs = New-Object System.Collections.ArrayList

Add-SettingDiffs -Diffs $diffs -Scope "dispatcherAppSettings" -BaselineMap $baseline.dispatcherAppSettings -CandidateMap $candidate.dispatcherAppSettings
Add-SettingDiffs -Diffs $diffs -Scope "executorAppSettings" -BaselineMap $baseline.executorAppSettings -CandidateMap $candidate.executorAppSettings

$baselineSchema = [string]$baseline.sqlSchemaText
$candidateSchema = [string]$candidate.sqlSchemaText
if ($baselineSchema -ne $candidateSchema) {
    [void]$diffs.Add([ordered]@{ severity = "warning"; scope = "sqlSchemaText"; name = "schema"; kind = "changed" })
}

$result = [ordered]@{
    baselineEnvironment = $baseline.environmentName
    candidateEnvironment = $candidate.environmentName
    comparedUtc = [DateTimeOffset]::UtcNow.ToString("o")
    diffCount = $diffs.Count
    diffs = @($diffs)
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = Resolve-FullPath -Path $OutputPath
    $outputDirectory = Split-Path -Path $outputFullPath -Parent
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }
    $result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
    Write-Host "Runtime environment comparison written to $outputFullPath"
}
else {
    $result | ConvertTo-Json -Depth 20
}

if ($diffs.Count -gt 0) {
    exit 1
}
