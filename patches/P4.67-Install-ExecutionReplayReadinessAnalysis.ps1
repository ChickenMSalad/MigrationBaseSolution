[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.67] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)
    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source)) { throw ("Payload file not found: {0}" -f $source) }
    if (-not $Apply) { Write-Step ("WOULD copy {0}" -f $RelativePath); return }
    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-LineOnce {
    param([string]$Path, [string]$Line, [string]$Anchor)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Line)) { Write-Step ("Already present: {0}" -f $Line); return }
    if (-not $content.Contains($Anchor)) { throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor) }
    if (-not $Apply) { Write-Step ("WOULD add line {0}" -f $Line); return }
    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line {0}" -f $Line)
}

function Add-UiReplaySupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay analysis UI support"
        return
    }

    if (-not $content.Contains("analyzeExecutionReplayReadiness")) {
        $content = $content.Replace(
            "import { buildExecutionDiagnosticBundleUrl } from './executionDiagnosticExportApi';",
            "import { buildExecutionDiagnosticBundleUrl } from './executionDiagnosticExportApi';`r`nimport { analyzeExecutionReplayReadiness } from './executionReplayApi';`r`nimport type { ExecutionReplayAnalysisResult } from './executionReplayTypes';")
    }

    if (-not $content.Contains("const [replayAnalysis, setReplayAnalysis]")) {
        $content = $content.Replace(
            "const [queueSummary, setQueueSummary] = useState<ExecutionWorkItemQueueSummary | null>(null);",
            "const [queueSummary, setQueueSummary] = useState<ExecutionWorkItemQueueSummary | null>(null);`r`n  const [replayAnalysis, setReplayAnalysis] = useState<ExecutionReplayAnalysisResult | null>(null);")
    }

    if (-not $content.Contains("async function analyzeSelectedReplayReadiness()")) {
        $marker = "async function pauseSelectedSession()"
        $block = @'
  async function analyzeSelectedReplayReadiness() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await analyzeExecutionReplayReadiness(selectedSession.executionSessionId);
      setReplayAnalysis(result);
      setStatusMessage(`Replay analysis complete. Risk score: ${result.riskScore}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze replay readiness.');
    }
  }

'@
        if (-not $content.Contains($marker)) {
            throw ("UI marker not found: {0}" -f $marker)
        }

        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Analyze replay")) {
        $content = $content.Replace(
            '<button type="button" onClick={exportSelectedDiagnostics}>Export diagnostics</button>',
            '<button type="button" onClick={exportSelectedDiagnostics}>Export diagnostics</button>`r`n            <button type="button" onClick={analyzeSelectedReplayReadiness}>Analyze replay</button>')
    }

    if (-not $content.Contains("<h3>Replay readiness</h3>")) {
        $marker = '<div className="filter-row">'
        $insert = @'
          {replayAnalysis ? (
            <div className="table-shell">
              <h3>Replay readiness</h3>
              <div className="metric-grid">
                <article><span>Risk score</span><strong>{replayAnalysis.riskScore}</strong></article>
                <article><span>Recommended</span><strong>{replayAnalysis.replayRecommended ? 'Yes' : 'No'}</strong></article>
                <article><span>Findings</span><strong>{replayAnalysis.findings.length}</strong></article>
                <article><span>Events</span><strong>{replayAnalysis.stateSummary.operationalEventCount}</strong></article>
              </div>
              <p>{replayAnalysis.recommendation}</p>
              <table>
                <thead><tr><th>Severity</th><th>Code</th><th>Finding</th></tr></thead>
                <tbody>
                  {replayAnalysis.findings.map((finding) => (
                    <tr key={`${finding.severity}-${finding.code}-${finding.message}`}>
                      <td>{finding.severity}</td>
                      <td><code>{finding.code}</code></td>
                      <td>{finding.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

'@
        $idx = $content.IndexOf($marker, $content.IndexOf("Analyze replay"))
        if ($idx -lt 0) {
            throw "Unable to locate insertion point for replay readiness panel."
        }

        $content = $content.Insert($idx, $insert)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay analysis UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAnalysisModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayAnalysisService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayAnalysisService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAnalysisEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayApi.ts"
Copy-PayloadFile "docs/operations/P4.67-execution-replay-readiness-analysis.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce `
    -Path $programPath `
    -Line "builder.Services.AddScoped<IExecutionReplayAnalysisService, SqlExecutionReplayAnalysisService>();" `
    -Anchor "builder.Services.AddScoped<IExecutionDiagnosticExportService, SqlExecutionDiagnosticExportService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce `
    -Path $compositionPath `
    -Line "        endpoints.MapExecutionReplayAnalysisEndpoints();" `
    -Anchor "        endpoints.MapExecutionDiagnosticExportEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplaySupport -Path $workspacePath

Write-Step "Complete."
