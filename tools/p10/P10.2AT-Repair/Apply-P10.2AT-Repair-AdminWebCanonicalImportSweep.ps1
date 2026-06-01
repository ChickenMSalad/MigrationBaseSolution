param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if (Test-Path -Path (Join-Path -Path $current -ChildPath '.git') -PathType Container) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Could not find repository root. Run this script from inside the MigrationBaseSolution repository.'
}

function Test-PathSegment {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Segment
    )

    $parts = @($Path -split '[\\/]+')
    foreach ($part in $parts) {
        if ($part -eq $Segment) {
            return $true
        }
    }

    return $false
}

function Convert-ToForwardSlash {
    param([Parameter(Mandatory = $true)][string]$Value)
    return ($Value -replace '\\', '/')
}

function Get-RelativeImportPath {
    param(
        [Parameter(Mandatory = $true)][string]$FromFile,
        [Parameter(Mandatory = $true)][string]$ToFile
    )

    $fromDirectory = [System.IO.Path]::GetDirectoryName($FromFile)
    $fromUri = New-Object System.Uri(($fromDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar))
    $toUri = New-Object System.Uri($ToFile)
    $relative = [System.Uri]::UnescapeDataString($fromUri.MakeRelativeUri($toUri).ToString())
    $relative = Convert-ToForwardSlash -Value $relative

    $relative = $relative -replace '\.tsx$', ''
    $relative = $relative -replace '\.ts$', ''
    $relative = $relative -replace '\.jsx$', ''
    $relative = $relative -replace '\.js$', ''
    $relative = $relative -replace '/index$', ''

    if (-not $relative.StartsWith('.')) {
        $relative = './' + $relative
    }

    return $relative
}

function Resolve-ImportTarget {
    param(
        [Parameter(Mandatory = $true)][string]$FromFile,
        [Parameter(Mandatory = $true)][string]$ImportSource
    )

    if (-not $ImportSource.StartsWith('.')) {
        return $null
    }

    $fromDirectory = [System.IO.Path]::GetDirectoryName($FromFile)
    $combined = [System.IO.Path]::GetFullPath((Join-Path -Path $fromDirectory -ChildPath $ImportSource))

    $candidatePaths = @(
        $combined,
        ($combined + '.ts'),
        ($combined + '.tsx'),
        ($combined + '.js'),
        ($combined + '.jsx'),
        ($combined + '.css'),
        (Join-Path -Path $combined -ChildPath 'index.ts'),
        (Join-Path -Path $combined -ChildPath 'index.tsx'),
        (Join-Path -Path $combined -ChildPath 'index.js'),
        (Join-Path -Path $combined -ChildPath 'index.jsx')
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -Path $candidatePath -PathType Leaf) {
            return $candidatePath
        }
    }

    return $null
}

function Find-ReplacementTarget {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$FromFile,
        [Parameter(Mandatory = $true)][string]$ImportSource
    )

    $leaf = [System.IO.Path]::GetFileName($ImportSource)
    if ([string]::IsNullOrWhiteSpace($leaf)) {
        return $null
    }

    $patterns = @(
        ($leaf + '.ts'),
        ($leaf + '.tsx'),
        ($leaf + '.js'),
        ($leaf + '.jsx'),
        ($leaf + '.css')
    )

    $allCandidates = New-Object System.Collections.Generic.List[string]
    foreach ($pattern in $patterns) {
        $items = @(Get-ChildItem -Path $SourceRoot -Recurse -File -Filter $pattern | Where-Object {
            -not (Test-PathSegment -Path $_.FullName -Segment 'node_modules') -and
            -not (Test-PathSegment -Path $_.FullName -Segment 'bin') -and
            -not (Test-PathSegment -Path $_.FullName -Segment 'obj')
        })

        foreach ($item in $items) {
            [void]$allCandidates.Add($item.FullName)
        }
    }

    $candidateArray = @($allCandidates.ToArray())
    if ($candidateArray.Length -eq 0) {
        return $null
    }

    $fromDirectory = [System.IO.Path]::GetDirectoryName($FromFile)
    $ranked = @(
        $candidateArray | Sort-Object @{
            Expression = {
                $relative = Get-RelativeImportPath -FromFile $FromFile -ToFile $_
                return $relative.Length
            }
        }, @{
            Expression = {
                if ($_ -like '*\features\*') { return 0 }
                return 1
            }
        }, @{
            Expression = { return $_ }
        }
    )

    if ($ranked.Length -eq 0) {
        return $null
    }

    return $ranked[0]
}

function Update-ImportSources {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[string]]$ReportLines
    )

    if (-not (Test-Path -Path $FilePath -PathType Leaf)) {
        return
    }

    $content = Get-Content -Path $FilePath -Raw
    $originalContent = $content

    $pattern = "(?m)(import\s+(?:type\s+)?(?:[\s\S]*?)\s+from\s+['""])(\.[^'""]+)(['""];?)"
    $matches = @([regex]::Matches($content, $pattern))
    if ($matches.Length -eq 0) {
        return
    }

    for ($index = $matches.Length - 1; $index -ge 0; $index--) {
        $match = $matches[$index]
        $importSource = $match.Groups[2].Value

        $resolved = Resolve-ImportTarget -FromFile $FilePath -ImportSource $importSource
        if ($null -ne $resolved) {
            continue
        }

        $replacementTarget = Find-ReplacementTarget -SourceRoot $SourceRoot -FromFile $FilePath -ImportSource $importSource
        if ($null -eq $replacementTarget) {
            [void]$ReportLines.Add(('- unresolved: {0} imports {1}' -f (Convert-ToForwardSlash -Value $FilePath), $importSource))
            continue
        }

        $newImportSource = Get-RelativeImportPath -FromFile $FilePath -ToFile $replacementTarget
        $replacement = $match.Groups[1].Value + $newImportSource + $match.Groups[3].Value
        $content = $content.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
        [void]$ReportLines.Add(('- updated: {0}: {1} -> {2}' -f (Convert-ToForwardSlash -Value $FilePath), $importSource, $newImportSource))
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $FilePath -Value $content -NoNewline
        Write-Host ('Updated imports: {0}' -f $FilePath)
    }
}

$repoRoot = Get-RepoRoot
$adminWebRoot = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'src' -ChildPath (Join-Path -Path 'Admin' -ChildPath 'Migration.Admin.Web'))
$sourceRoot = Join-Path -Path $adminWebRoot -ChildPath 'src'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$reportLines = New-Object System.Collections.Generic.List[string]
[void]$reportLines.Add('# P10.2AT-Repair Admin Web Canonical Import Sweep')
[void]$reportLines.Add('')
[void]$reportLines.Add('This report was generated from the local working tree.')
[void]$reportLines.Add('')

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.ts' -or $_.Extension -eq '.tsx') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'node_modules') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'bin') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'obj')
})

foreach ($sourceFile in $sourceFiles) {
    Update-ImportSources -FilePath $sourceFile.FullName -SourceRoot $sourceRoot -ReportLines $reportLines
}

$reportPath = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'docs' -ChildPath (Join-Path -Path 'P10' -ChildPath 'P10.2AT-Repair-AdminWebCanonicalImportSweep.md'))
$reportDirectory = [System.IO.Path]::GetDirectoryName($reportPath)
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}

Set-Content -Path $reportPath -Value ($reportLines -join [Environment]::NewLine)

Write-Host ('P10.2AT-Repair canonical import sweep completed. Report: {0}' -f $reportPath)
