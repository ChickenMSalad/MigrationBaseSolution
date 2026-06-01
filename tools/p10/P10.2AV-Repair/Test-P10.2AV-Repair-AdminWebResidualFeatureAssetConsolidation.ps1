Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $start = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($start)) { $start = (Get-Location).Path }
    $current = [System.IO.DirectoryInfo]::new($start)
    while ($null -ne $current) {
        $srcPath = [System.IO.Path]::Combine($current.FullName, 'src')
        if (Test-Path -LiteralPath $srcPath -PathType Container) { return $current.FullName }
        $current = $current.Parent
    }
    throw 'Unable to locate repository root.'
}

function Test-VariableColonPattern {
    param([Parameter(Mandatory = $true)][string]$Text)
    for ($i = 0; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -ne '$') { continue }
        $next = $i + 1
        if ($next -ge $Text.Length) { continue }
        $c = $Text[$next]
        $isStart = (($c -ge 'A') -and ($c -le 'Z')) -or (($c -ge 'a') -and ($c -le 'z')) -or ($c -eq '_')
        if (-not $isStart) { continue }
        $j = $next + 1
        while ($j -lt $Text.Length) {
            $d = $Text[$j]
            $isPart = (($d -ge 'A') -and ($d -le 'Z')) -or (($d -ge 'a') -and ($d -le 'z')) -or (($d -ge '0') -and ($d -le '9')) -or ($d -eq '_')
            if (-not $isPart) { break }
            $j++
        }
        if (($j -lt $Text.Length) -and ($Text[$j] -eq ':')) { return $true }
    }
    return $false
}

$repoRoot = Get-RepoRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2AV-Repair', 'AdminWebResidualFeatureAssetConsolidationRepair.md')

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { throw ('Canonical Admin Web src root not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) { throw ('Canonical Admin Web features root not found: {0}' -f $featuresRoot) }
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw ('Expected P10.2AV repair report was not found: {0}' -f $reportPath) }

$featureFiles = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -File -Include *.ts,*.tsx)
foreach ($file in $featureFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content.Contains('../../../../api/')) { throw ('Legacy deep flat API import remains in feature file: {0}' -f $file.FullName) }
    if ($content.Contains('../../../../types/')) { throw ('Legacy deep flat types import remains in feature file: {0}' -f $file.FullName) }
}

$scriptRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AV-Repair')
$scriptFiles = @(Get-ChildItem -LiteralPath $scriptRoot -File -Filter *.ps1)
foreach ($script in $scriptFiles) {
    $text = Get-Content -LiteralPath $script.FullName -Raw
    if (Test-VariableColonPattern -Text $text) { throw ('Unsafe PowerShell variable-colon interpolation pattern found in {0}' -f $script.FullName) }
    $bytes = [System.IO.File]::ReadAllBytes($script.FullName)
    foreach ($byte in $bytes) {
        if (($byte -lt 32) -and ($byte -ne 9) -and ($byte -ne 10) -and ($byte -ne 13)) { throw ('Unexpected control character found in {0}' -f $script.FullName) }
    }
}
Write-Host 'P10.2AV repair validation passed.'
