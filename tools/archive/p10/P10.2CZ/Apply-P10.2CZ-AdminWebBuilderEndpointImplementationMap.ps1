Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CZ'
New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$reportPath = Join-Path $docsRoot 'P10.2CZ-AdminWebBuilderEndpointImplementationMap.md'
$csvPath = Join-Path $artifactRoot 'builder-endpoint-implementation-map.csv'

$report = New-Object 'System.Collections.Generic.List[string]'
$rows = New-Object 'System.Collections.Generic.List[string]'
[void]$rows.Add('Area,Kind,PathOrFile,Status,Notes')

[void]$report.Add('# P10.2CZ - Admin Web Builder Endpoint Implementation Map')
[void]$report.Add('')
[void]$report.Add('This report is generated from the local repo state. It does not add fake endpoints and does not mutate application source.')
[void]$report.Add('')
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root exists: `{0}`' -f (Test-Path $adminWebRoot)))
[void]$report.Add('')

$expectedEndpoints = @(
    @{ Area = 'Manifest Builder'; Verb = 'POST'; Path = '/api/manifest-builder/build' },
    @{ Area = 'Manifest Builder'; Verb = 'POST'; Path = '/api/manifest-builder/validate' },
    @{ Area = 'Manifest Builder'; Verb = 'POST'; Path = '/api/manifest-builder/preview' },
    @{ Area = 'Taxonomy Builder'; Verb = 'GET'; Path = '/api/taxonomy-builder' },
    @{ Area = 'Taxonomy Builder'; Verb = 'POST'; Path = '/api/taxonomy-builder/validate' },
    @{ Area = 'Taxonomy Builder'; Verb = 'POST'; Path = '/api/taxonomy-builder/preview' },
    @{ Area = 'Mapping Builder'; Verb = 'GET'; Path = '/api/mapping-builder' },
    @{ Area = 'Mapping Builder'; Verb = 'GET'; Path = '/api/mapping-profiles' },
    @{ Area = 'Mapping Builder'; Verb = 'POST'; Path = '/api/mapping-builder/validate' },
    @{ Area = 'Mapping Builder'; Verb = 'POST'; Path = '/api/mapping-builder/preview' }
)

[void]$report.Add('## Expected Builder API Surface')
[void]$report.Add('')
foreach ($endpoint in $expectedEndpoints) {
    $line = ('- `{0} {1}`' -f $endpoint.Verb, $endpoint.Path)
    [void]$report.Add($line)
    [void]$rows.Add(('{0},ExpectedEndpoint,{1} {2},Expected,From builder UI/runtime smoke' -f $endpoint.Area, $endpoint.Verb, $endpoint.Path))
}
[void]$report.Add('')

$allCsFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
$foundCs = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and
    $_.FullName -notmatch '\\obj\\' -and
    $_.FullName -notmatch '\\.git\\'
}
foreach ($file in $foundCs) { [void]$allCsFiles.Add($file) }

$programFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
foreach ($file in $allCsFiles) {
    if ($file.Name -eq 'Program.cs') { [void]$programFiles.Add($file) }
}

[void]$report.Add('## Discovered Host Program Files')
[void]$report.Add('')
if ($programFiles.Count -eq 0) {
    [void]$report.Add('- No `Program.cs` files discovered.')
    [void]$rows.Add('Host,Program.cs,,Missing,No Program.cs files discovered')
} else {
    foreach ($file in $programFiles) {
        $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\')
        [void]$report.Add(('- `{0}`' -f $relative))
        [void]$rows.Add(('Host,Program.cs,{0},Discovered,Potential endpoint registration host' -f $relative.Replace(',', ';')))
    }
}
[void]$report.Add('')

$keywords = @('manifest-builder', 'ManifestBuilder', 'taxonomy-builder', 'TaxonomyBuilder', 'mapping-builder', 'MappingBuilder', 'mapping-profiles', 'MappingProfiles')
$matches = New-Object 'System.Collections.Generic.List[object]'
foreach ($file in $allCsFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }
    foreach ($keyword in $keywords) {
        if ($content.IndexOf($keyword, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            [void]$matches.Add([PSCustomObject]@{ File = $file.FullName; Keyword = $keyword })
        }
    }
}

[void]$report.Add('## Backend Builder-Related Source Candidates')
[void]$report.Add('')
if ($matches.Count -eq 0) {
    [void]$report.Add('- No backend C# files mention builder route/class keywords.')
    [void]$rows.Add('Backend,BuilderKeyword,,Missing,No backend C# files mention builder route/class keywords')
} else {
    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($match in $matches) {
        $relative = $match.File.Substring($repoRoot.Length).TrimStart('\')
        $key = ('{0}|{1}' -f $relative, $match.Keyword)
        if ($seen.Add($key)) {
            [void]$report.Add(('- `{0}` contains `{1}`' -f $relative, $match.Keyword))
            [void]$rows.Add(('Backend,BuilderKeyword,{0},Discovered,{1}' -f $relative.Replace(',', ';'), $match.Keyword.Replace(',', ';')))
        }
    }
}
[void]$report.Add('')

$webApiFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
if (Test-Path $sourceRoot) {
    $foundTs = Get-ChildItem -Path $sourceRoot -Recurse -Include '*.ts','*.tsx' -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '\\node_modules\\' -and
        $_.FullName -notmatch '\\dist\\' -and
        $_.FullName -notmatch '\\reference\\'
    }
    foreach ($file in $foundTs) { [void]$webApiFiles.Add($file) }
}

[void]$report.Add('## Admin Web Builder API Usage Candidates')
[void]$report.Add('')
$webHits = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in $webApiFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }
    foreach ($endpoint in $expectedEndpoints) {
        if ($content.IndexOf($endpoint.Path, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\')
            $value = ('{0}|{1}' -f $relative, $endpoint.Path)
            [void]$webHits.Add($value)
            [void]$report.Add(('- `{0}` references `{1}`' -f $relative, $endpoint.Path))
            [void]$rows.Add(('{0},AdminWebUsage,{1},Discovered,{2}' -f $endpoint.Area, $relative.Replace(',', ';'), $endpoint.Path))
        }
    }
}
if ($webHits.Count -eq 0) {
    [void]$report.Add('- No exact expected builder endpoint string references found in compiled Admin Web source.')
    [void]$rows.Add('AdminWeb,BuilderEndpointUsage,,Missing,No exact expected endpoint string references found')
}
[void]$report.Add('')

[void]$report.Add('## Recommended Next Implementation Step')
[void]$report.Add('')
[void]$report.Add('Use this report to add route aliases only where backend services or endpoint classes already exist. Do not add placeholder endpoint implementations just to satisfy UI smoke checks.')
[void]$report.Add('')
[void]$report.Add('Observed from prior smoke: manifest build exists but needs a valid POST payload; taxonomy and mapping builder paths returned 404 and need route discovery or real backend endpoint registration.')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Set-Content -LiteralPath $csvPath -Value $rows.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote CSV: {0}' -f $csvPath)
Write-Host 'P10.2CZ Admin Web builder endpoint implementation map applied.'
