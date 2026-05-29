[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $LeftSnapshotPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $RightSnapshotPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return [System.IO.Path]::Combine((Get-Location).Path, $Path)
}

$leftPath = Resolve-FullPath -Path $LeftSnapshotPath
$rightPath = Resolve-FullPath -Path $RightSnapshotPath
if (-not (Test-Path -LiteralPath $leftPath)) { throw ('Left snapshot not found: {0}' -f $leftPath) }
if (-not (Test-Path -LiteralPath $rightPath)) { throw ('Right snapshot not found: {0}' -f $rightPath) }

$left = Get-Content -LiteralPath $leftPath -Raw | ConvertFrom-Json
$right = Get-Content -LiteralPath $rightPath -Raw | ConvertFrom-Json

$leftKeys = @{}
foreach ($item in @($left.objects)) {
    $key = '{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}' -f $item.itemType,$item.schemaName,$item.objectName,$item.columnName,$item.dataType,$item.isNullable,$item.isIdentity,$item.relatedObject
    $leftKeys[$key] = $true
}

$rightKeys = @{}
foreach ($item in @($right.objects)) {
    $key = '{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}' -f $item.itemType,$item.schemaName,$item.objectName,$item.columnName,$item.dataType,$item.isNullable,$item.isIdentity,$item.relatedObject
    $rightKeys[$key] = $true
}

$leftOnly = @()
foreach ($key in $leftKeys.Keys) {
    if (-not $rightKeys.ContainsKey($key)) { $leftOnly += $key }
}

$rightOnly = @()
foreach ($key in $rightKeys.Keys) {
    if (-not $leftKeys.ContainsKey($key)) { $rightOnly += $key }
}

$result = [pscustomobject]@{
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    leftEnvironment = $left.environmentName
    rightEnvironment = $right.environmentName
    leftOnly = @($leftOnly | Sort-Object)
    rightOnly = @($rightOnly | Sort-Object)
    isEquivalent = (@($leftOnly).Count -eq 0 -and @($rightOnly).Count -eq 0)
}

$json = ConvertTo-Json -InputObject $result -Depth 10
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $json
} else {
    $outPath = Resolve-FullPath -Path $OutputPath
    $parent = Split-Path -Parent $outPath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    Set-Content -LiteralPath $outPath -Value $json -Encoding UTF8
    Write-Host ('Runtime SQL snapshot comparison written to {0}' -f $outPath)
}
