[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

$file = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx'
if (-not (Test-Path -LiteralPath $file)) {
    throw 'TaxonomyBuilder.tsx not found.'
}

$content = Get-Content -LiteralPath $file -Raw
if ($null -eq $content) {
    throw 'TaxonomyBuilder.tsx was empty or unreadable.'
}

$required = @(
    'import { apiPost } from "../../../../../api/core/adminApiClient";',
    'apiPost<BuildTaxonomyArtifactRequest, BuildTaxonomyArtifactResponse>(path, payload)',
    '/api/taxonomy-builder/build',
    '/api/taxonomy-builder/blank-template'
)

foreach ($marker in $required) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing required marker: ' + $marker)
    }
}

$forbidden = @(
    'import { apiRequest } from "../../../../../api/core/adminApiClient";',
    'body: JSON.stringify(payload)'
)

foreach ($marker in $forbidden) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden marker found: ' + $marker)
    }
}

$replacementChar = [char]0xfffd
if ($content.IndexOf($replacementChar) -ge 0) {
    throw 'Replacement character corruption found.'
}

Write-Host 'Taxonomy Builder JSON POST repair validation passed.'
