[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $PlanPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $AppName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath,

    [Parameter(Mandatory = $false)]
    [switch] $IncludeTransitionalDeletes
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return (Join-Path (Get-Location).Path $Path)
}

$planFullPath = Resolve-FullPath -Path $PlanPath
if (-not (Test-Path -LiteralPath $planFullPath)) {
    throw ('Plan file not found: {0}' -f $planFullPath)
}

$plan = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $planFullPath -Raw)
$keysToDelete = New-Object System.Collections.Generic.List[string]

foreach ($key in @($plan.legacyKeysToReviewForDeletion)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$key)) {
        $keysToDelete.Add([string]$key) | Out-Null
    }
}

if ($IncludeTransitionalDeletes) {
    foreach ($key in @($plan.transitionalKeysToReviewForDeletion)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$key)) {
            $keysToDelete.Add([string]$key) | Out-Null
        }
    }
}

$outputFullPath = Resolve-FullPath -Path $OutputPath
$parent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('[CmdletBinding()]') | Out-Null
$lines.Add('param()') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Set-StrictMode -Version 2.0') | Out-Null
$lines.Add('$ErrorActionPreference = ''Stop''') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('$resourceGroup = ''{0}''' -f ($ResourceGroup -replace '''', ''''''))) | Out-Null
$lines.Add(('$appName = ''{0}''' -f ($AppName -replace '''', ''''''))) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('# Review this generated script before running it. It deletes only known stale runtime setting keys from one App Service.') | Out-Null
$lines.Add('') | Out-Null

if (@($keysToDelete).Count -eq 0) {
    $lines.Add('Write-Host ''No stale runtime AppSettings were found for deletion.''') | Out-Null
}
else {
    foreach ($key in @($keysToDelete | Sort-Object -Unique)) {
        $escapedKey = $key -replace '''', ''''''
        $lines.Add(('Write-Host ''Deleting AppSetting {0} from '' $appName' -f $escapedKey)) | Out-Null
        $lines.Add('az webapp config appsettings delete `') | Out-Null
        $lines.Add('  --resource-group $resourceGroup `') | Out-Null
        $lines.Add('  --name $appName `') | Out-Null
        $lines.Add(('  --setting-names ''{0}''' -f $escapedKey)) | Out-Null
        $lines.Add('') | Out-Null
    }
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Generated reviewable AppSettings cleanup command script: {0}' -f $outputFullPath)
