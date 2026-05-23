[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.68] {0}" -f $Message) }

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

function Add-UiReplayPreparationSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay preparation UI support"
        return
    }

    if (-not $content.Contains("prepareExecutionReplayManifest")) {
        $anchor = "import { analyzeExecutionReplayReadiness } from './executionReplayApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace(
            $anchor,
            $anchor + "`r`nimport { prepareExecutionReplayManifest } from './executionReplayPreparationApi';`r`nimport type { ExecutionReplayPreparationResult } from './executionReplayPreparationTypes';")
    }

    if (-not $content.Contains("const [replayPreparation, setReplayPreparation]")) {
        $anchor = "const [replayAnalysis, setReplayAnalysis]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $insertAt = $lineEnd + 1
        $content = $content.Insert($insertAt, "`r`n  const [replayPreparation, setReplayPreparation] = useState<ExecutionReplayPreparationResult | null>(null);`r`n  const [replayScope, setReplayScope] = useState('failed-only');")
    }

    if (-not $content.Contains("async function prepareSelectedReplayManifest()")) {
        $marker = "async function pauseSelectedSession()"
        $block = @'
  async function prepareSelectedReplayManifest() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await prepareExecutionReplayManifest({
        executionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        reason: controlReason || null,
      });

      setReplayPreparation(result);
      setStatusMessage(`Replay manifest prepared with ${result.items.length} item(s).`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to prepare replay manifest.');
    }
  }

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Prepare replay manifest")) {
        $anchor = '<button type="button" onClick={analyzeSelectedReplayReadiness}>Analyze replay</button>'
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $replacement = @'
<button type="button" onClick={analyzeSelectedReplayReadiness}>Analyze replay</button>
            <label>
              Replay scope
              <select value={replayScope} onChange={(event) => setReplayScope(event.target.value)}>
                <option value="failed-only">failed-only</option>
                <option value="dead-letter-only">dead-letter-only</option>
                <option value="incomplete-only">incomplete-only</option>
                <option value="all">all</option>
              </select>
            </label>
            <button type="button" onClick={prepareSelectedReplayManifest}>Prepare replay manifest</button>
'@
        $content = $content.Replace($anchor, $replacement)
    }

    if (-not $content.Contains("<h3>Replay preparation manifest</h3>")) {
        $marker = "{replayAnalysis ? ("
        $insert = @'
          {replayPreparation ? (
            <div className="table-shell">
              <h3>Replay preparation manifest</h3>
              <div className="metric-grid">
                <article><span>Scope</span><strong>{replayPreparation.scope}</strong></article>
                <article><span>Can prepare</span><strong>{replayPreparation.canPrepareReplay ? 'Yes' : 'No'}</strong></article>
                <article><span>Approval</span><strong>{replayPreparation.requiresApproval ? 'Required' : 'Not required'}</strong></article>
                <article><span>Items</span><strong>{replayPreparation.items.length}</strong></article>
              </div>
              <p>{replayPreparation.recommendation}</p>
              <table>
                <thead><tr><th>Order</th><th>Type</th><th>Name</th><th>Source status</th></tr></thead>
                <tbody>
                  {replayPreparation.items.length === 0 ? (
                    <tr><td colSpan={4}>No replay items matched the selected scope.</td></tr>
                  ) : (
                    replayPreparation.items.map((item) => (
                      <tr key={`${item.replayOrder}-${item.sourceExecutionWorkItemId ?? item.replayName}`}>
                        <td>{item.replayOrder}</td>
                        <td>{item.replayType}</td>
                        <td>{item.replayName}</td>
                        <td>{item.sourceStatus}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay preparation UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayPreparationModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayPreparationService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPreparationService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayPreparationEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPreparationTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPreparationApi.ts"
Copy-PayloadFile "docs/operations/P4.68-execution-replay-preparation-manifest.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce `
    -Path $programPath `
    -Line "builder.Services.AddScoped<IExecutionReplayPreparationService, SqlExecutionReplayPreparationService>();" `
    -Anchor "builder.Services.AddScoped<IExecutionReplayAnalysisService, SqlExecutionReplayAnalysisService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce `
    -Path $compositionPath `
    -Line "        endpoints.MapExecutionReplayPreparationEndpoints();" `
    -Anchor "        endpoints.MapExecutionReplayAnalysisEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplayPreparationSupport -Path $workspacePath

Write-Step "Complete."
