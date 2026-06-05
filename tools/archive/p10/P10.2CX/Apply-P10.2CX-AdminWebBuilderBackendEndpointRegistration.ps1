Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminApiRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Api'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CX-AdminWebBuilderBackendEndpointRegistration.md'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CX'

if (-not (Test-Path -LiteralPath $adminApiRoot -PathType Container)) {
    throw ('Admin API root was not found: {0}' -f $adminApiRoot)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $reportPath) | Out-Null
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CX - Admin Web Builder Backend Endpoint Registration')
[void]$report.Add('')
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin API root: `{0}`' -f $adminApiRoot))
[void]$report.Add('')

$endpointFiles = New-Object 'System.Collections.Generic.List[object]'
$taxonomyPath = Join-Path $adminApiRoot 'Endpoints\TaxonomyBuilderEndpoints.cs'
$mappingPath = Join-Path $adminApiRoot 'Endpoints\MappingBuilderEndpoints.cs'

if (Test-Path -LiteralPath $taxonomyPath -PathType Leaf) {
    [void]$endpointFiles.Add([pscustomobject]@{
        Name = 'Taxonomy Builder'
        Method = 'MapTaxonomyBuilderEndpoints'
        File = $taxonomyPath
    })
    [void]$report.Add(('- Found Taxonomy Builder endpoint file: `{0}`' -f $taxonomyPath))
} else {
    [void]$report.Add(('- Taxonomy Builder endpoint file missing: `{0}`' -f $taxonomyPath))
}

if (Test-Path -LiteralPath $mappingPath -PathType Leaf) {
    [void]$endpointFiles.Add([pscustomobject]@{
        Name = 'Mapping Builder'
        Method = 'MapMappingBuilderEndpoints'
        File = $mappingPath
    })
    [void]$report.Add(('- Found Mapping Builder endpoint file: `{0}`' -f $mappingPath))
} else {
    [void]$report.Add(('- Mapping Builder endpoint file missing: `{0}`' -f $mappingPath))
}

if ($endpointFiles.Count -eq 0) {
    [void]$report.Add('')
    [void]$report.Add('No builder endpoint classes were found. No registration was attempted.')
    Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
    throw 'No Taxonomy or Mapping builder endpoint classes were found to register.'
}

$csFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.cs' -File | Where-Object {
    $fullName = $_.FullName
    $fullName -notmatch '\\bin\\' -and
    $fullName -notmatch '\\obj\\' -and
    $fullName -notmatch '\\.git\\'
}

$candidateFiles = New-Object 'System.Collections.Generic.List[object]'
foreach ($file in $csFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -like '*app.Map*' -and $content -like '*app.Run*') {
        [void]$candidateFiles.Add($file)
    }
}

if ($candidateFiles.Count -eq 0) {
    [void]$report.Add('')
    [void]$report.Add('No startup file containing both app.Map and app.Run was found.')
    Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
    throw 'Unable to locate Admin API startup file.'
}

$startupFile = $null
foreach ($file in $candidateFiles) {
    if ($file.FullName.StartsWith($adminApiRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $startupFile = $file
        break
    }
}
if ($null -eq $startupFile) {
    if ($candidateFiles.Count -eq 1) {
        $startupFile = $candidateFiles[0]
    } else {
        [void]$report.Add('')
        [void]$report.Add('Multiple startup candidates were found and none were under Admin API root:')
        foreach ($candidate in $candidateFiles) {
            [void]$report.Add(('- `{0}`' -f $candidate.FullName))
        }
        Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
        throw 'Unable to choose a startup file safely.'
    }
}

$startupPath = $startupFile.FullName
[void]$report.Add('')
[void]$report.Add(('Startup file selected: `{0}`' -f $startupPath))

$lines = New-Object 'System.Collections.Generic.List[string]'
$existingLines = Get-Content -LiteralPath $startupPath
foreach ($line in $existingLines) {
    [void]$lines.Add($line)
}

$changed = $false
$hasUsing = $false
foreach ($line in $lines) {
    if ($line.Trim() -eq 'using Migration.Admin.Api.Endpoints;') {
        $hasUsing = $true
        break
    }
}
if (-not $hasUsing) {
    $insertUsingIndex = 0
    while ($insertUsingIndex -lt $lines.Count -and $lines[$insertUsingIndex].Trim().StartsWith('using ')) {
        $insertUsingIndex++
    }
    $lines.Insert($insertUsingIndex, 'using Migration.Admin.Api.Endpoints;')
    $changed = $true
    [void]$report.Add('Added using Migration.Admin.Api.Endpoints; to startup file.')
}

foreach ($endpoint in $endpointFiles) {
    $method = [string]$endpoint.Method
    $alreadyRegistered = $false
    foreach ($line in $lines) {
        if ($line -like ('*{0}(*' -f $method)) {
            $alreadyRegistered = $true
            break
        }
    }

    if ($alreadyRegistered) {
        [void]$report.Add(('{0} already registered with {1}().' -f $endpoint.Name, $method))
        continue
    }

    $insertIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -like '*app.Run(*') {
            $insertIndex = $i
            break
        }
    }
    if ($insertIndex -lt 0) {
        Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
        throw ('Unable to find app.Run() insertion point for {0}.' -f $endpoint.Name)
    }

    $registrationLine = ('app.{0}();' -f $method)
    $lines.Insert($insertIndex, $registrationLine)
    $changed = $true
    [void]$report.Add(('Registered {0}: `{1}`' -f $endpoint.Name, $registrationLine))
}

if ($changed) {
    Set-Content -LiteralPath $startupPath -Value $lines.ToArray() -Encoding UTF8
    Write-Host ('Updated Admin API startup file: {0}' -f $startupPath)
} else {
    Write-Host ('No Admin API startup changes needed: {0}' -f $startupPath)
}

[void]$report.Add('')
[void]$report.Add('## Endpoint smoke expectations')
[void]$report.Add('')
[void]$report.Add('- `/api/taxonomy-builder/build` should no longer return 404 when Admin API is restarted if TaxonomyBuilderEndpoints.cs exists.')
[void]$report.Add('- `/api/mapping-builder/manifests/{artifactId}/columns` should no longer return 404 when Admin API is restarted if MappingBuilderEndpoints.cs exists.')
[void]$report.Add('- POST/action endpoints may still return 400/405 from generic smoke checks when called without valid request bodies; that is not the same as route missing.')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CX Admin Web builder backend endpoint registration applied.'
