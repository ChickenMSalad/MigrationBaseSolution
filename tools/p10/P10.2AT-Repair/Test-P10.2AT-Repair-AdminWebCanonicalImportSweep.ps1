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

function Resolve-ImportTarget {
    param(
        [Parameter(Mandatory = $true)][string]$FromFile,
        [Parameter(Mandatory = $true)][string]$ImportSource
    )

    if (-not $ImportSource.StartsWith('.')) {
        return $true
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
            return $true
        }
    }

    return $false
}

$repoRoot = Get-RepoRoot
$sourceRoot = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'src' -ChildPath (Join-Path -Path 'Admin' -ChildPath (Join-Path -Path 'Migration.Admin.Web' -ChildPath 'src')))
$reportPath = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'docs' -ChildPath (Join-Path -Path 'P10' -ChildPath 'P10.2AT-Repair-AdminWebCanonicalImportSweep.md'))

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.ts' -or $_.Extension -eq '.tsx') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'node_modules') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'bin') -and
    -not (Test-PathSegment -Path $_.FullName -Segment 'obj')
})

$unresolved = New-Object System.Collections.Generic.List[string]
$pattern = "(?m)import\s+(?:type\s+)?(?:[\s\S]*?)\s+from\s+['""](\.[^'""]+)['""];?"

foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    $matches = @([regex]::Matches($content, $pattern))

    foreach ($match in $matches) {
        $importSource = $match.Groups[1].Value
        $isResolved = Resolve-ImportTarget -FromFile $sourceFile.FullName -ImportSource $importSource
        if (-not $isResolved) {
            [void]$unresolved.Add(('{0} -> {1}' -f $sourceFile.FullName, $importSource))
        }
    }
}

$unresolvedArray = @($unresolved.ToArray())
if ($unresolvedArray.Length -gt 0) {
    $message = 'Unresolved relative imports remain:' + [Environment]::NewLine + ($unresolvedArray -join [Environment]::NewLine)
    throw $message
}

Write-Host 'P10.2AT-Repair validation passed.'
