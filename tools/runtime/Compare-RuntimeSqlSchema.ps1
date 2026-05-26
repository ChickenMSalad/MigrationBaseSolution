[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExpectedPath,

    [Parameter(Mandatory = $true)]
    [string]$ActualPath,

    [Parameter(Mandatory = $false)]
    [string]$DiffOutputPath = ".\runtime-sql-schema-diff.txt"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ExistingFilePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "File not found: $resolved"
    }

    return $resolved
}

$expected = Get-ExistingFilePath -Path $ExpectedPath
$actual = Get-ExistingFilePath -Path $ActualPath
$diffPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DiffOutputPath)
$diffDirectory = Split-Path -Parent $diffPath
if (-not [string]::IsNullOrWhiteSpace($diffDirectory)) {
    New-Item -ItemType Directory -Force -Path $diffDirectory | Out-Null
}

$expectedLines = Get-Content -LiteralPath $expected
$actualLines = Get-Content -LiteralPath $actual
$diff = Compare-Object -ReferenceObject $expectedLines -DifferenceObject $actualLines -SyncWindow 20

if ($null -eq $diff -or @($diff).Count -eq 0) {
    "No schema differences found." | Set-Content -LiteralPath $diffPath -Encoding UTF8
    Write-Host "No schema differences found."
    exit 0
}

$diff |
    Select-Object SideIndicator, InputObject |
    Format-Table -AutoSize |
    Out-String -Width 4096 |
    Set-Content -LiteralPath $diffPath -Encoding UTF8

Write-Error "Runtime SQL schema differences found. See $diffPath"
exit 1
