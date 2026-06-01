Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $start = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($start)) { $start = (Get-Location).Path }
    $current = [System.IO.DirectoryInfo]::new($start)
    while ($null -ne $current) {
        $gitPath = [System.IO.Path]::Combine($current.FullName, '.git')
        $srcPath = [System.IO.Path]::Combine($current.FullName, 'src')
        if ((Test-Path -LiteralPath $gitPath) -or (Test-Path -LiteralPath $srcPath -PathType Container)) { return $current.FullName }
        $current = $current.Parent
    }
    throw 'Unable to locate repository root.'
}

function Get-RelativeImportPath {
    param(
        [Parameter(Mandatory = $true)][string]$FromFile,
        [Parameter(Mandatory = $true)][string]$ToFile
    )
    $fromDirectory = [System.IO.Path]::GetDirectoryName($FromFile)
    $fromBase = $fromDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $fromUri = [System.Uri]::new($fromBase)
    $toUri = [System.Uri]::new($ToFile)
    $relative = $fromUri.MakeRelativeUri($toUri).ToString()
    $relative = [System.Uri]::UnescapeDataString($relative)
    $relative = $relative -replace '\\', '/'
    $relative = $relative -replace '\.tsx$', ''
    $relative = $relative -replace '\.ts$', ''
    if (-not $relative.StartsWith('.')) { $relative = './' + $relative }
    return $relative
}

function Move-IfNeeded {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][System.Collections.ArrayList]$Report
    )
    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        [void]$Report.Add(('- Already present: {0}' -f $Destination))
        return $false
    }
    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        [void]$Report.Add(('- Source absent: {0}' -f $Source))
        return $false
    }
    $destinationDirectory = [System.IO.Path]::GetDirectoryName($Destination)
    if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }
    Move-Item -LiteralPath $Source -Destination $Destination
    [void]$Report.Add(('- Moved: {0} -> {1}' -f $Source, $Destination))
    return $true
}

function Get-ImportSourceFromLine {
    param([Parameter(Mandatory = $true)][string]$Line)
    $fromToken = ' from '
    $fromIndex = $Line.IndexOf($fromToken, [System.StringComparison]::Ordinal)
    if ($fromIndex -lt 0) { return $null }
    $startSearch = $fromIndex + $fromToken.Length
    if ($startSearch -ge $Line.Length) { return $null }
    $quoteCharacters = @([char]39, [char]34)
    foreach ($quote in $quoteCharacters) {
        $quoteIndex = $Line.IndexOf($quote, $startSearch)
        if ($quoteIndex -lt 0) { continue }
        $sourceStart = $quoteIndex + 1
        if ($sourceStart -ge $Line.Length) { continue }
        $sourceEnd = $Line.IndexOf($quote, $sourceStart)
        if ($sourceEnd -le $sourceStart) { continue }
        $source = $Line.Substring($sourceStart, ($sourceEnd - $sourceStart))
        return [pscustomobject]@{
            Source = $source
            SourceStart = $sourceStart
            SourceLength = ($sourceEnd - $sourceStart)
        }
    }
    return $null
}

function Get-FeatureRootForFile {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$FeaturesRoot
    )
    $cursor = $File.Directory
    while ($null -ne $cursor) {
        if (($null -ne $cursor.Parent) -and ($cursor.Parent.FullName -eq $FeaturesRoot)) { return $cursor.FullName }
        if ($cursor.FullName -eq $FeaturesRoot) { return $File.Directory.FullName }
        $cursor = $cursor.Parent
    }
    return $File.Directory.FullName
}

function Resolve-ImportTarget {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFile,
        [Parameter(Mandatory = $true)][string]$ImportSource
    )
    $sourceDirectory = [System.IO.Path]::GetDirectoryName($SourceFile)
    $relativePath = $ImportSource.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $candidateBase = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($sourceDirectory, $relativePath))
    $candidateFiles = @(
        ($candidateBase + '.ts'),
        ($candidateBase + '.tsx'),
        ([System.IO.Path]::Combine($candidateBase, 'index.ts')),
        ([System.IO.Path]::Combine($candidateBase, 'index.tsx'))
    )
    foreach ($candidate in $candidateFiles) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    return $null
}

function Update-FeatureImports {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][System.Collections.ArrayList]$Report
    )
    $featuresRoot = [System.IO.Path]::Combine($SourceRoot, 'features')
    if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) { throw ('Feature root not found: {0}' -f $featuresRoot) }
    $sourceFiles = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -File -Include *.ts,*.tsx)
    foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $lines = @($content -split "`r?`n", 0)
        $changed = $false
        for ($index = 0; $index -lt $lines.Length; $index++) {
            $line = $lines[$index]
            $importInfo = Get-ImportSourceFromLine -Line $line
            if ($null -eq $importInfo) { continue }
            $importSource = [string]$importInfo.Source
            if (-not $importSource.StartsWith('.')) { continue }
            $existingTarget = Resolve-ImportTarget -SourceFile $file.FullName -ImportSource $importSource
            if ($null -ne $existingTarget) { continue }
            $leaf = [System.IO.Path]::GetFileName(($importSource.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
            if ([string]::IsNullOrWhiteSpace($leaf)) { continue }
            $featureRootForFile = Get-FeatureRootForFile -File $file -FeaturesRoot $featuresRoot
            $possibleTargets = @(
                [System.IO.Path]::Combine($featureRootForFile, 'api', ($leaf + '.ts')),
                [System.IO.Path]::Combine($featureRootForFile, 'types', ($leaf + '.ts')),
                [System.IO.Path]::Combine($featureRootForFile, 'pages', ($leaf + '.tsx'))
            )
            $replacementTarget = $null
            foreach ($possible in $possibleTargets) {
                if (Test-Path -LiteralPath $possible -PathType Leaf) { $replacementTarget = $possible; break }
            }
            if ($null -eq $replacementTarget) { continue }
            $newImportSource = Get-RelativeImportPath -FromFile $file.FullName -ToFile $replacementTarget
            if ($newImportSource -eq $importSource) { continue }
            $before = $line.Substring(0, [int]$importInfo.SourceStart)
            $afterStart = [int]$importInfo.SourceStart + [int]$importInfo.SourceLength
            $after = $line.Substring($afterStart)
            $lines[$index] = $before + $newImportSource + $after
            $changed = $true
            [void]$Report.Add(('- Updated import in {0}: {1} -> {2}' -f $file.FullName, $importSource, $newImportSource))
        }
        if ($changed) {
            $newContent = [string]::Join("`r`n", $lines)
            Set-Content -LiteralPath $file.FullName -Value $newContent -NoNewline
        }
    }
}

$repoRoot = Get-RepoRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$apiRoot = [System.IO.Path]::Combine($sourceRoot, 'api')
$typesRoot = [System.IO.Path]::Combine($sourceRoot, 'types')
$artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2AV-Repair')
$reportPath = [System.IO.Path]::Combine($artifactRoot, 'AdminWebResidualFeatureAssetConsolidationRepair.md')

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { throw ('Canonical Admin Web src root not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) { throw ('Canonical Admin Web features root not found: {0}' -f $featuresRoot) }
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) { New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null }

$report = [System.Collections.ArrayList]::new()
[void]$report.Add('# P10.2AV Repair - Admin Web Residual Feature Asset Consolidation')
[void]$report.Add('')
[void]$report.Add(('Repository root: {0}' -f $repoRoot))
[void]$report.Add('')

$featureDirectories = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -Directory | Where-Object {
    (Test-Path -LiteralPath ([System.IO.Path]::Combine($_.FullName, 'pages')) -PathType Container) -or
    (Test-Path -LiteralPath ([System.IO.Path]::Combine($_.FullName, 'api')) -PathType Container) -or
    (Test-Path -LiteralPath ([System.IO.Path]::Combine($_.FullName, 'types')) -PathType Container)
})

$managed = [System.Collections.ArrayList]::new()
foreach ($featureDirectory in $featureDirectories) {
    $featureName = $featureDirectory.Name
    if ($featureName -in @('api','types','pages','components')) { continue }
    $apiFileName = $featureName + 'Api.ts'
    $typeFileName = $featureName + '.ts'
    $flatApi = [System.IO.Path]::Combine($apiRoot, $apiFileName)
    $flatType = [System.IO.Path]::Combine($typesRoot, $typeFileName)
    $destApi = [System.IO.Path]::Combine($featureDirectory.FullName, 'api', $apiFileName)
    $destType = [System.IO.Path]::Combine($featureDirectory.FullName, 'types', $typeFileName)
    [void]$managed.Add([pscustomobject]@{
        Feature = $featureName
        FeaturePath = $featureDirectory.FullName
        FlatApi = $flatApi
        DestinationApi = $destApi
        FlatType = $flatType
        DestinationType = $destType
    })
}

[void]$report.Add('## Managed feature folders')
foreach ($item in @($managed)) { [void]$report.Add(('- {0}: {1}' -f $item.Feature, $item.FeaturePath)) }
[void]$report.Add('')
[void]$report.Add('## Moves')
foreach ($item in @($managed)) {
    [void](Move-IfNeeded -Source $item.FlatApi -Destination $item.DestinationApi -Report $report)
    [void](Move-IfNeeded -Source $item.FlatType -Destination $item.DestinationType -Report $report)
}
[void]$report.Add('')
[void]$report.Add('## Import normalization')
Update-FeatureImports -SourceRoot $sourceRoot -Report $report
[void]$report.Add('')
[void]$report.Add('P10.2AV repair apply completed.')
Set-Content -LiteralPath $reportPath -Value $report -Encoding UTF8
Write-Host ('P10.2AV repair applied. Report: {0}' -f $reportPath)
