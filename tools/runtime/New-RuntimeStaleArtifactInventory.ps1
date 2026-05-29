[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-stale-artifact-quarantine.sample.json')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function ConvertTo-NormalizedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return ($Path -replace '\\', '/')
}

function Test-PathFragmentMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string] $NormalizedPath,

        [Parameter(Mandatory = $false)]
        [string[]] $Fragments = @()
    )

    foreach ($fragment in @($Fragments)) {
        if ([string]::IsNullOrWhiteSpace($fragment)) {
            continue
        }

        $normalizedFragment = ConvertTo-NormalizedPath -Path $fragment
        if ($NormalizedPath.IndexOf($normalizedFragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

$configFullPath = $ConfigurationPath
if (-not [System.IO.Path]::IsPathRooted($configFullPath)) {
    $configFullPath = Join-Path $RepoRoot $configFullPath
}

if (-not (Test-Path -LiteralPath $configFullPath)) {
    throw ('Configuration file not found: {0}' -f $configFullPath)
}

$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json

$includePathFragments = @()
if ($null -ne $config.PSObject.Properties['includePathFragments']) {
    $includePathFragments = @($config.includePathFragments)
}

$excludePathFragments = @()
if ($null -ne $config.PSObject.Properties['excludePathFragments']) {
    $excludePathFragments = @($config.excludePathFragments)
}

$staleReferenceTerms = @()
if ($null -ne $config.PSObject.Properties['staleReferenceTerms']) {
    $staleReferenceTerms = @($config.staleReferenceTerms)
}

$repoRootFullPath = (Resolve-Path $RepoRoot).Path
$results = @()

$files = Get-ChildItem -LiteralPath $repoRootFullPath -Recurse -File -ErrorAction Stop
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($repoRootFullPath.Length).TrimStart('\', '/')
    $normalizedRelativePath = ConvertTo-NormalizedPath -Path $relativePath

    if (Test-PathFragmentMatch -NormalizedPath $normalizedRelativePath -Fragments $excludePathFragments) {
        continue
    }

    $isIncludedByPath = Test-PathFragmentMatch -NormalizedPath $normalizedRelativePath -Fragments $includePathFragments
    if (-not $isIncludedByPath) {
        continue
    }

    $matchedTerms = @()
    $text = $null

    try {
        $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
    }
    catch {
        $text = $null
    }

    if ($null -ne $text) {
        foreach ($term in $staleReferenceTerms) {
            if ([string]::IsNullOrWhiteSpace($term)) {
                continue
            }

            if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $matchedTerms += $term
            }
        }
    }

    if ($matchedTerms.Count -gt 0 -or $normalizedRelativePath.IndexOf('.migration-control-plane', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $results += [pscustomobject]@{
            Path = $relativePath
            Length = $file.Length
            LastWriteTimeUtc = $file.LastWriteTimeUtc.ToString('o')
            MatchedTerms = ($matchedTerms -join '; ')
        }
    }
}

$results | Sort-Object Path
