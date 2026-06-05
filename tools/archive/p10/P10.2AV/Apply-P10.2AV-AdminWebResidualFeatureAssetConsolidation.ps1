Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $start = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($start)) {
        $start = (Get-Location).Path
    }

    $current = [System.IO.DirectoryInfo]::new($start)
    while ($null -ne $current) {
        $gitPath = [System.IO.Path]::Combine($current.FullName, '.git')
        $srcPath = [System.IO.Path]::Combine($current.FullName, 'src')
        if ((Test-Path -LiteralPath $gitPath) -or (Test-Path -LiteralPath $srcPath)) {
            return $current.FullName
        }
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
    $fromUri = [System.Uri]::new(($fromDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar))
    $toUri = [System.Uri]::new($ToFile)
    $relative = $fromUri.MakeRelativeUri($toUri).ToString()
    $relative = [System.Uri]::UnescapeDataString($relative)
    $relative = $relative -replace '\\', '/'
    $relative = $relative -replace '\.tsx$', ''
    $relative = $relative -replace '\.ts$', ''
    if (-not $relative.StartsWith('.')) {
        $relative = './' + $relative
    }
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

function Update-FeatureImports {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][System.Collections.ArrayList]$Report
    )

    $featuresRoot = [System.IO.Path]::Combine($SourceRoot, 'features')
    if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) {
        throw ('Feature root not found: {0}' -f $featuresRoot)
    }

    $sourceFiles = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -File -Include *.ts,*.tsx)
    foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $original = $content
        $importMatches = @([regex]::Matches($content, "from\s+['\"]([^'\"]+)['\"]"))
        if ($importMatches.Length -eq 0) {
            continue
        }

        foreach ($match in $importMatches) {
            if ($match.Groups.Count -lt 2) {
                continue
            }

            $importSource = $match.Groups[1].Value
            if (-not $importSource.StartsWith('.')) {
                continue
            }

            $candidateBase = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($file.FullName), ($importSource -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
            $candidateFiles = @(
                ($candidateBase + '.ts'),
                ($candidateBase + '.tsx'),
                ([System.IO.Path]::Combine($candidateBase, 'index.ts')),
                ([System.IO.Path]::Combine($candidateBase, 'index.tsx'))
            )

            $targetFile = $null
            foreach ($candidate in $candidateFiles) {
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    $targetFile = $candidate
                    break
                }
            }

            if ($null -ne $targetFile) {
                continue
            }

            $leaf = [System.IO.Path]::GetFileName($candidateBase)
            if ([string]::IsNullOrWhiteSpace($leaf)) {
                continue
            }

            $featureRootForFile = $file.Directory.FullName
            $cursor = $file.Directory
            while (($null -ne $cursor) -and ($cursor.Parent.FullName -ne $featuresRoot)) {
                $cursor = $cursor.Parent
            }
            if ($null -ne $cursor) {
                $featureRootForFile = $cursor.FullName
            }

            $possibleTargets = @(
                [System.IO.Path]::Combine($featureRootForFile, 'api', ($leaf + '.ts')),
                [System.IO.Path]::Combine($featureRootForFile, 'types', ($leaf + '.ts')),
                [System.IO.Path]::Combine($featureRootForFile, 'pages', ($leaf + '.tsx'))
            )

            $replacementTarget = $null
            foreach ($possible in $possibleTargets) {
                if (Test-Path -LiteralPath $possible -PathType Leaf) {
                    $replacementTarget = $possible
                    break
                }
            }

            if ($null -eq $replacementTarget) {
                continue
            }

            $newImportSource = Get-RelativeImportPath -FromFile $file.FullName -ToFile $replacementTarget
            if ($newImportSource -ne $importSource) {
                $escaped = [regex]::Escape($match.Value)
                $replacement = $match.Value.Replace($importSource, $newImportSource)
                $content = [regex]::Replace($content, $escaped, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement }, 1)
                [void]$Report.Add(('- Updated import in {0}: {1} -> {2}' -f $file.FullName, $importSource, $newImportSource))
            }
        }

        if ($content -ne $original) {
            Set-Content -LiteralPath $file.FullName -Value $content -NoNewline
        }
    }
}

$repoRoot = Get-RepoRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$apiRoot = [System.IO.Path]::Combine($sourceRoot, 'api')
$typesRoot = [System.IO.Path]::Combine($sourceRoot, 'types')
$artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2AV')
$reportPath = [System.IO.Path]::Combine($artifactRoot, 'AdminWebResidualFeatureAssetConsolidation.md')

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Canonical Admin Web src root not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) {
    throw ('Canonical Admin Web features root not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$report = [System.Collections.ArrayList]::new()
[void]$report.Add('# P10.2AV - Admin Web Residual Feature Asset Consolidation')
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
    if ($featureName -in @('api','types','pages','components')) {
        continue
    }

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
foreach ($item in @($managed)) {
    [void]$report.Add(('- {0}: {1}' -f $item.Feature, $item.FeaturePath))
}
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
[void]$report.Add('P10.2AV apply completed.')
Set-Content -LiteralPath $reportPath -Value $report -Encoding UTF8
Write-Host ('P10.2AV Admin Web residual feature asset consolidation applied. Report: {0}' -f $reportPath)
