Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')

$toolsDir = Join-Path $repoRoot 'tools\p10\P10.2CM'
$docsDir = Join-Path $repoRoot 'docs\P10'
$artifactsDir = Join-Path $repoRoot 'artifacts\p10\P10.2CM'

New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

$runnerPath = Join-Path $toolsDir 'Run-P10.2CM-AdminWebAdminApiEndpointAlignment.ps1'
$runnerContent = @'
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$adminWebSourceRoot = Join-Path $adminWebRoot 'src'
$srcRoot = Join-Path $repoRoot 'src'
$docsDir = Join-Path $repoRoot 'docs\P10'
$artifactsDir = Join-Path $repoRoot 'artifacts\p10\P10.2CM'

New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CM - Admin Web Admin API Endpoint Alignment')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date).ToUniversalTime()))
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('Admin Web source root: `{0}`' -f $adminWebSourceRoot))
[void]$report.Add(('Source root: `{0}`' -f $srcRoot))
[void]$report.Add('')

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $adminWebSourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $adminWebSourceRoot)
}

$webFiles = @(Get-ChildItem -LiteralPath $adminWebSourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $_.FullName -notmatch '\\node_modules\\' -and
    $_.FullName -notmatch '\\dist\\' -and
    $_.FullName -notmatch '\\reference\\'
})

$endpointRegex = [regex]'(?<quote>["''])(?<path>/(?:api/)?[A-Za-z0-9_./{}?=&:%-]+)\k<quote>'
$webEndpointLines = New-Object 'System.Collections.Generic.List[object]'
$endpointSet = New-Object 'System.Collections.Generic.HashSet[string]'

foreach ($file in $webFiles) {
    $relativeFile = $file.FullName.Substring($adminWebSourceRoot.Length).TrimStart('\')
    $lines = @(Get-Content -LiteralPath $file.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $lineText = [string]$lines[$i]
        if ([string]::IsNullOrWhiteSpace($lineText)) { continue }
        $matches = @($endpointRegex.Matches($lineText))
        foreach ($match in $matches) {
            $path = [string]$match.Groups['path'].Value
            if ([string]::IsNullOrWhiteSpace($path)) { continue }
            if ($path.StartsWith('//')) { continue }
            if ($path -eq '/') { continue }
            [void]$endpointSet.Add($path)
            $webEndpointLines.Add([pscustomobject]@{
                File = $relativeFile
                Line = ($i + 1)
                Path = $path
            }) | Out-Null
        }
    }
}

$controllerFiles = @(Get-ChildItem -LiteralPath $srcRoot -Recurse -File -Include '*.cs' | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and
    $_.FullName -notmatch '\\obj\\' -and
    $_.FullName -notmatch '\\node_modules\\'
})

$routeAttributeRegex = [regex]'\[(?:HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch|Route)\s*(?:\(\s*"(?<route>[^"]*)"\s*\))?\]'
$controllerEndpointLines = New-Object 'System.Collections.Generic.List[object]'
$controllerRouteSet = New-Object 'System.Collections.Generic.HashSet[string]'

foreach ($file in $controllerFiles) {
    $relativeFile = $file.FullName.Substring($srcRoot.Length).TrimStart('\')
    $lines = @(Get-Content -LiteralPath $file.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $lineText = [string]$lines[$i]
        if ([string]::IsNullOrWhiteSpace($lineText)) { continue }
        $matches = @($routeAttributeRegex.Matches($lineText))
        foreach ($match in $matches) {
            $route = [string]$match.Groups['route'].Value
            if ([string]::IsNullOrWhiteSpace($route)) {
                $route = '(attribute-without-template)'
            }
            [void]$controllerRouteSet.Add($route)
            $controllerEndpointLines.Add([pscustomobject]@{
                File = $relativeFile
                Line = ($i + 1)
                Route = $route
            }) | Out-Null
        }
    }
}

[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- Admin Web TypeScript files scanned: {0}' -f $webFiles.Length))
[void]$report.Add(('- Admin Web endpoint-like references found: {0}' -f $webEndpointLines.Count))
[void]$report.Add(('- Unique Admin Web endpoint-like paths found: {0}' -f $endpointSet.Count))
[void]$report.Add(('- C# files scanned: {0}' -f $controllerFiles.Length))
[void]$report.Add(('- Controller route attributes found: {0}' -f $controllerEndpointLines.Count))
[void]$report.Add(('- Unique controller route templates found: {0}' -f $controllerRouteSet.Count))
[void]$report.Add('')

[void]$report.Add('## Admin Web endpoint-like references')
[void]$report.Add('')
if ($webEndpointLines.Count -eq 0) {
    [void]$report.Add('_No endpoint-like string references were found in compiled Admin Web source._')
} else {
    foreach ($item in @($webEndpointLines | Sort-Object File, Line, Path)) {
        [void]$report.Add(('- `{0}:{1}` -> `{2}`' -f $item.File, $item.Line, $item.Path))
    }
}
[void]$report.Add('')

[void]$report.Add('## Admin API/server route-like declarations')
[void]$report.Add('')
if ($controllerEndpointLines.Count -eq 0) {
    [void]$report.Add('_No C# route attributes were found under `src`._')
} else {
    foreach ($item in @($controllerEndpointLines | Sort-Object File, Line, Route)) {
        [void]$report.Add(('- `{0}:{1}` -> `{2}`' -f $item.File, $item.Line, $item.Route))
    }
}
[void]$report.Add('')

[void]$report.Add('## Alignment notes')
[void]$report.Add('')
[void]$report.Add('- This report is intentionally heuristic. It inventories endpoint-like references and route-like declarations; it does not claim semantic compatibility.')
[void]$report.Add('- Use this as the source map for the next API-contract alignment sets.')
[void]$report.Add('- The compiled Admin Web source should not import from `reference` or `/apps`; this runner does not mutate source.')
[void]$report.Add('')

$docPath = Join-Path $docsDir 'P10.2CM-AdminWebAdminApiEndpointAlignment.md'
$artifactPath = Join-Path $artifactsDir 'admin-web-admin-api-endpoint-alignment.md'
Set-Content -LiteralPath $docPath -Value $report -Encoding UTF8
Set-Content -LiteralPath $artifactPath -Value $report -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $docPath)
Write-Host ('Wrote artifact report: {0}' -f $artifactPath)
'@

Set-Content -LiteralPath $runnerPath -Value $runnerContent -Encoding UTF8

& powershell -ExecutionPolicy Bypass -File $runnerPath

Write-Host 'P10.2CM Admin Web Admin API endpoint alignment applied.'
