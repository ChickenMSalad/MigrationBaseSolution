Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2CS-AdminWebBuilderWorkspaceRestoration.md'

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw ('Admin Web App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot | Out-Null
}

$specs = @(
    [pscustomobject]@{
        Key = 'manifest'
        Title = 'Manifest Builder'
        Component = 'ManifestBuilder'
        Folder = 'manifest'
        FileName = 'ManifestBuilder.tsx'
        Route = '/builders/manifest'
        Endpoint = '/api/manifest-builder/build'
        Description = 'Build and validate migration manifests through the Admin API.'
    },
    [pscustomobject]@{
        Key = 'taxonomy'
        Title = 'Taxonomy Builder'
        Component = 'TaxonomyBuilder'
        Folder = 'taxonomy'
        FileName = 'TaxonomyBuilder.tsx'
        Route = '/builders/taxonomy'
        Endpoint = '/api/taxonomy-builder/build'
        Description = 'Build and validate migration taxonomy definitions through the Admin API.'
    },
    [pscustomobject]@{
        Key = 'mapping'
        Title = 'Mapping Builder'
        Component = 'MappingBuilder'
        Folder = 'mapping'
        FileName = 'MappingBuilder.tsx'
        Route = '/builders/mapping'
        Endpoint = '/api/mapping-builder/build'
        Description = 'Build and validate source-to-target mapping definitions through the Admin API.'
    }
)

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CS - Admin Web Builder Workspace Restoration')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add('')
[void]$report.Add('## Actions')
[void]$report.Add('')

$appLines = New-Object 'System.Collections.Generic.List[string]'
foreach ($line in [System.IO.File]::ReadAllLines($appPath)) {
    [void]$appLines.Add($line)
}

$appText = [System.IO.File]::ReadAllText($appPath)
$routeInsertIndex = -1
for ($i = 0; $i -lt $appLines.Count; $i++) {
    if ($appLines[$i].Contains('</Routes>')) {
        $routeInsertIndex = $i
        break
    }
}

if ($routeInsertIndex -lt 0) {
    throw ('Unable to locate </Routes> in {0}; refusing to update routes.' -f $appPath)
}

$lastImportIndex = -1
for ($i = 0; $i -lt $appLines.Count; $i++) {
    $trimmed = $appLines[$i].TrimStart()
    if ($trimmed.StartsWith('import ')) {
        $lastImportIndex = $i
    }
}

if ($lastImportIndex -lt 0) {
    throw ('Unable to locate import block in {0}; refusing to update imports.' -f $appPath)
}

foreach ($spec in $specs) {
    $pageDir = Join-Path $sourceRoot ('features\builders\{0}\pages' -f $spec.Folder)
    $pagePath = Join-Path $pageDir $spec.FileName

    if (-not (Test-Path -LiteralPath $pageDir -PathType Container)) {
        New-Item -ItemType Directory -Path $pageDir | Out-Null
    }

    if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
        $pageContent = @"
import { useState } from 'react';

type BuilderStatus = 'idle' | 'running' | 'success' | 'error';

export function $($spec.Component)() {
  const [status, setStatus] = useState<BuilderStatus>('idle');
  const [message, setMessage] = useState<string>('Ready to run.');
  const [details, setDetails] = useState<string>('');

  async function runBuilder() {
    setStatus('running');
    setMessage('Calling $($spec.Endpoint)...');
    setDetails('');

    try {
      const response = await fetch('$($spec.Endpoint)', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      const responseText = await response.text();
      setDetails(responseText || '(empty response)');

      if (!response.ok) {
        setStatus('error');
        setMessage(`Request failed with HTTP ${response.status}.`);
        return;
      }

      setStatus('success');
      setMessage('Builder request completed successfully.');
    } catch (error) {
      setStatus('error');
      setMessage(error instanceof Error ? error.message : 'Builder request failed.');
    }
  }

  return (
    <section className="page-stack">
      <header className="page-header">
        <p className="eyebrow">Migration Builder</p>
        <h1>$($spec.Title)</h1>
        <p>$($spec.Description)</p>
      </header>

      <div className="card">
        <h2>Runtime action</h2>
        <p>
          Endpoint: <code>$($spec.Endpoint)</code>
        </p>
        <button type="button" onClick={runBuilder} disabled={status === 'running'}>
          {status === 'running' ? 'Running...' : 'Run $($spec.Title)'}
        </button>
      </div>

      <div className="card">
        <h2>Status</h2>
        <p>{message}</p>
        {details ? <pre>{details}</pre> : null}
      </div>
    </section>
  );
}
"@
        Set-Content -LiteralPath $pagePath -Value $pageContent -Encoding UTF8
        [void]$report.Add(('- Created `{0}`.' -f $pagePath))
    } else {
        [void]$report.Add(('- Existing page preserved: `{0}`.' -f $pagePath))
    }

    $importPath = ('./features/builders/{0}/pages/{1}' -f $spec.Folder, ($spec.FileName -replace '\.tsx$', ''))
    $importLine = ('import {{ {0} }} from "{1}";' -f $spec.Component, $importPath)
    if (-not $appText.Contains($importPath)) {
        $appLines.Insert(($lastImportIndex + 1), $importLine)
        $lastImportIndex = $lastImportIndex + 1
        $routeInsertIndex = $routeInsertIndex + 1
        $appText = $appText + [Environment]::NewLine + $importLine
        [void]$report.Add(('- Added App.tsx import for `{0}`.' -f $spec.Component))
    } else {
        [void]$report.Add(('- App.tsx already imports `{0}`.' -f $spec.Component))
    }

    $routeNeedle = ('path="{0}"' -f $spec.Route)
    $routeLine = ('          <Route path="{0}" element={{<{1} />}} />' -f $spec.Route, $spec.Component)
    if (-not $appText.Contains($routeNeedle)) {
        $appLines.Insert($routeInsertIndex, $routeLine)
        $routeInsertIndex = $routeInsertIndex + 1
        $appText = $appText + [Environment]::NewLine + $routeLine
        [void]$report.Add(('- Added App.tsx route `{0}`.' -f $spec.Route))
    } else {
        [void]$report.Add(('- App.tsx already has route `{0}`.' -f $spec.Route))
    }
}

Set-Content -LiteralPath $appPath -Value $appLines.ToArray() -Encoding UTF8

[void]$report.Add('')
[void]$report.Add('## Builder routes')
[void]$report.Add('')
foreach ($spec in $specs) {
    [void]$report.Add(('- `{0}` -> `{1}` -> `{2}`' -f $spec.Title, $spec.Route, $spec.Endpoint))
}
[void]$report.Add('')
[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- These pages restore reachability for the builder workflows and call the expected Admin API builder endpoints with POST.')
[void]$report.Add('- Existing canonical builder pages are preserved if already present.')
[void]$report.Add('- No reference-tree files are imported from compiled Admin Web source.')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CS Admin Web builder workspace restoration applied.'
