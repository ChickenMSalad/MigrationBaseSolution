[CmdletBinding()]
param(
    [string]$RepoRoot = ".",
    [string]$AllowlistPath = ".\config-samples\runtime-legacy-reference-allowlist.sample.json"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-RepoRootPath {
    param([string]$Path)
    return (Resolve-Path -LiteralPath $Path).Path
}

function Convert-ToRepoRelativePath {
    param(
        [string]$Root,
        [string]$Path
    )
    return $Path.Substring($Root.Length).TrimStart('\', '/')
}

function Test-IgnoredPath {
    param([string]$Path)
    $normalized = $Path -replace '/', '\'
    $parts = $normalized -split '\\'
    return ($parts -contains 'bin' -or $parts -contains 'obj' -or $parts -contains '.git' -or $parts -contains 'artifacts')
}

function Test-AllowlistedPath {
    param(
        [string]$RelativePath,
        [string[]]$AllowedPathFragments
    )

    $normalized = $RelativePath.Replace('\', '/')
    foreach ($fragment in $AllowedPathFragments) {
        if ([string]::IsNullOrWhiteSpace($fragment)) { continue }
        $normalizedFragment = $fragment.Replace('\', '/')
        if ($normalized.StartsWith($normalizedFragment, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
        if ($normalized.Equals($normalizedFragment, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

$resolvedRoot = Resolve-RepoRootPath -Path $RepoRoot
$resolvedAllowlistPath = Resolve-Path -LiteralPath $AllowlistPath
$allowlist = Get-Content -LiteralPath $resolvedAllowlistPath.Path -Raw | ConvertFrom-Json

$legacyTerms = @($allowlist.legacyTerms)
$allowedFragments = @($allowlist.allowedPathFragments)
$extensions = @('.cs', '.sql', '.json', '.ps1', '.csproj')
$violations = New-Object System.Collections.Generic.List[object]

Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File | ForEach-Object {
    $file = $_
    if (Test-IgnoredPath -Path $file.FullName) { return }
    if ($extensions -notcontains $file.Extension) { return }

    $relative = Convert-ToRepoRelativePath -Root $resolvedRoot -Path $file.FullName
    if (Test-AllowlistedPath -RelativePath $relative -AllowedPathFragments $allowedFragments) { return }

    $lineNumber = 0
    Get-Content -LiteralPath $file.FullName | ForEach-Object {
        $lineNumber++
        $line = $_
        foreach ($term in $legacyTerms) {
            if ($line.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $violations.Add([pscustomobject]@{
                    Path = $relative
                    LineNumber = $lineNumber
                    Term = $term
                    Line = $line.Trim()
                }) | Out-Null
            }
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | Format-Table -AutoSize | Out-String | Write-Host
    throw ("Found {0} non-allowlisted legacy runtime references." -f $violations.Count)
}

Write-Host "Legacy runtime reference boundary validation passed."
