[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ComparisonPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return [System.IO.Path]::Combine((Get-Location).Path, $Path)
}

$comparisonFullPath = Resolve-FullPath -Path $ComparisonPath
if (-not (Test-Path -LiteralPath $comparisonFullPath)) {
    throw ('Comparison file not found: {0}' -f $comparisonFullPath)
}

$comparison = Get-Content -LiteralPath $comparisonFullPath -Raw | ConvertFrom-Json
$outputFullPath = Resolve-FullPath -Path $OutputPath
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$lines = @()
$lines += '# Runtime Local/Azure Parity Report'
$lines += ''
$lines += ('- Generated UTC: {0}' -f [DateTimeOffset]::UtcNow.ToString('o'))
$lines += ('- Left environment: {0}' -f $comparison.leftEnvironment)
$lines += ('- Right environment: {0}' -f $comparison.rightEnvironment)
$lines += ('- Equivalent: {0}' -f $comparison.isEquivalent)
$lines += ''
$lines += '## Left-only differences'
$lines += ''
if (@($comparison.leftOnly).Count -eq 0) {
    $lines += '- None'
} else {
    foreach ($item in @($comparison.leftOnly)) { $lines += ('- `{0}`' -f $item) }
}
$lines += ''
$lines += '## Right-only differences'
$lines += ''
if (@($comparison.rightOnly).Count -eq 0) {
    $lines += '- None'
} else {
    foreach ($item in @($comparison.rightOnly)) { $lines += ('- `{0}`' -f $item) }
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime parity report written to {0}' -f $outputFullPath)
