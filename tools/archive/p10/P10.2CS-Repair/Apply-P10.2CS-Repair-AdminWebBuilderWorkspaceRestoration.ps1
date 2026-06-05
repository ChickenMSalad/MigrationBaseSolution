Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$docsDir = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsDir 'P10.2CS-Repair-AdminWebBuilderWorkspaceRestoration.md'

if (-not (Test-Path -LiteralPath $srcRoot)) { throw ('Admin Web src root was not found: {0}' -f $srcRoot) }
if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx was not found: {0}' -f $appPath) }
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null

$builders = @(
    @{ Name='Manifest Builder'; Symbol='ManifestBuilder'; Route='/manifest-builder'; Import='./features/governance/manifestBuilder/pages/ManifestBuilder'; File='features\governance\manifestBuilder\pages\ManifestBuilder.tsx'; ApiPath='/api/manifest-builder/build' },
    @{ Name='Taxonomy Builder'; Symbol='TaxonomyBuilder'; Route='/taxonomy-builder'; Import='./features/governance/taxonomyBuilder/pages/TaxonomyBuilder'; File='features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx'; ApiPath='/api/taxonomy-builder/build' },
    @{ Name='Mapping Builder'; Symbol='MappingBuilder'; Route='/mapping-builder'; Import='./features/governance/mappingBuilder/pages/MappingBuilder'; File='features\governance\mappingBuilder\pages\MappingBuilder.tsx'; ApiPath='/api/mapping-builder/save' }
)

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CS Repair - Admin Web Builder Workspace Restoration')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add('')

function New-BuilderPageContent {
    param(
        [Parameter(Mandatory=$true)][string]$Symbol,
        [Parameter(Mandatory=$true)][string]$Title,
        [Parameter(Mandatory=$true)][string]$ApiPath
    )
    $content = @'
import { useState } from "react";
import { Card } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";

type ProbeState = "idle" | "running" | "success" | "error";

type BuilderWorkspaceProps = {
  title: string;
  apiPath: string;
};

function BuilderWorkspace({ title, apiPath }: BuilderWorkspaceProps) {
  const [status, setStatus] = useState<ProbeState>("idle");
  const [message, setMessage] = useState<string | null>(null);

  async function probeEndpoint() {
    setStatus("running");
    setMessage(null);

    try {
      const response = await fetch(apiPath, { method: "OPTIONS" });
      if (response.ok || response.status === 204 || response.status === 405) {
        setStatus("success");
        setMessage("Builder endpoint is reachable. HTTP " + String(response.status) + ".");
        return;
      }

      setStatus("error");
      setMessage("Request failed with HTTP " + String(response.status) + ".");
    } catch (error) {
      setStatus("error");
      setMessage(error instanceof Error ? error.message : String(error));
    }
  }

  return (
    <div className="pageStack">
      <Card title={title} description="Builder workspace restored from the Admin Web consolidation pass.">
        <p>
          This workspace is reachable from the canonical Admin Web route. Use the endpoint probe to confirm the Admin API route is visible in the local stack before wiring deeper builder workflows.
        </p>
        <div className="detailGrid">
          <span>API endpoint</span>
          <strong>{apiPath}</strong>
          <span>Status</span>
          <strong>{status}</strong>
        </div>
        <div className="buttonRow">
          <button type="button" className="primaryButton" onClick={() => void probeEndpoint()} disabled={status === "running"}>
            {status === "running" ? "Checking..." : "Check endpoint"}
          </button>
        </div>
      </Card>
      {status === "error" && message && <LoadingError message={message} />}
      {status === "success" && message && <Card title="Endpoint reachable" message={message} />}
    </div>
  );
}

export function __SYMBOL__() {
  return <BuilderWorkspace title="__TITLE__" apiPath="__API_PATH__" />;
}
'@
    $content = $content.Replace('__SYMBOL__', $Symbol)
    $content = $content.Replace('__TITLE__', $Title)
    $content = $content.Replace('__API_PATH__', $ApiPath)
    return $content
}

foreach ($builder in $builders) {
    $builderPath = Join-Path $srcRoot $builder.File
    $builderDir = Split-Path -Parent $builderPath
    New-Item -ItemType Directory -Force -Path $builderDir | Out-Null

    $shouldWrite = $false
    if (-not (Test-Path -LiteralPath $builderPath)) {
        $shouldWrite = $true
        [void]$report.Add(('- Created missing page: `{0}`' -f $builder.File))
    } else {
        $existing = Get-Content -LiteralPath $builderPath -Raw
        if ($existing.Contains('$' + '{response.status}') -or $existing.Contains('Builder workspace restored from the Admin Web consolidation pass.')) {
            $shouldWrite = $true
            [void]$report.Add(('- Normalized generated workspace page: `{0}`' -f $builder.File))
        } else {
            [void]$report.Add(('- Preserved existing builder page: `{0}`' -f $builder.File))
        }
    }

    if ($shouldWrite) {
        $pageContent = New-BuilderPageContent -Symbol $builder.Symbol -Title $builder.Name -ApiPath $builder.ApiPath
        Set-Content -LiteralPath $builderPath -Value $pageContent -Encoding UTF8
    }
}

$appContent = Get-Content -LiteralPath $appPath -Raw
$appChanged = $false
foreach ($builder in $builders) {
    if ($appContent -notmatch [regex]::Escape($builder.Import)) {
        $importLine = ('import {{ {0} }} from "{1}";' -f $builder.Symbol, $builder.Import)
        $appContent = $importLine + [Environment]::NewLine + $appContent
        $appChanged = $true
        [void]$report.Add(('- Added App import for {0}.' -f $builder.Symbol))
    } else {
        [void]$report.Add(('- App import already present for {0}.' -f $builder.Symbol))
    }

    if ($appContent -notmatch [regex]::Escape($builder.Route)) {
        if ($appContent -match '</Routes>') {
            $routeLine = ('        <Route path="{0}" element={{<{1} />}} />' -f $builder.Route, $builder.Symbol)
            $appContent = $appContent -replace '</Routes>', ($routeLine + [Environment]::NewLine + '      </Routes>')
            $appChanged = $true
            [void]$report.Add(('- Added App route `{0}`.' -f $builder.Route))
        } else {
            [void]$report.Add(('- App.tsx did not contain `</Routes>`; route not added for `{0}`.' -f $builder.Route))
        }
    } else {
        [void]$report.Add(('- App route already present for `{0}`.' -f $builder.Route))
    }
}
if ($appChanged) { Set-Content -LiteralPath $appPath -Value $appContent -Encoding UTF8 }

[void]$report.Add('')
[void]$report.Add('## Safety')
[void]$report.Add('')
[void]$report.Add('- Generated TSX content uses string concatenation for HTTP status messages.')
[void]$report.Add('- Generated TSX payload is stored in a literal PowerShell here-string.')
[void]$report.Add('- Existing non-generated builder pages are preserved.')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CS Repair Admin Web builder workspace restoration applied.'
