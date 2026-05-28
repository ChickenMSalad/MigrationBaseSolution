[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $InputPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputZipPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$inputFullPath = if ([System.IO.Path]::IsPathRooted($InputPath)) { $InputPath } else { Join-Path (Get-Location).Path $InputPath }
$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputZipPath)) { $OutputZipPath } else { Join-Path (Get-Location).Path $OutputZipPath }

if (-not (Test-Path -LiteralPath $inputFullPath)) {
    throw ('Evidence input path not found: {0}' -f $InputPath)
}

$outputDirectory = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Force
}

Compress-Archive -LiteralPath $inputFullPath -DestinationPath $outputFullPath -Force
Write-Host ('Wrote runtime rebuild evidence bundle: {0}' -f $outputFullPath)
