[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.69] {0}" -f $Message) }

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

function Add-UiReplayMaterializationSupport {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $Apply) { Write-Step "WOULD add replay materialization UI support"; return }

    if (-not $content.Contains("materializeExecutionReplay")) {
        $anchor = "import { prepareExecutionReplayManifest } from './executionReplayPreparationApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, $anchor + "`r`nimport { materializeExecutionReplay } from './executionReplayMaterializationApi';`r`nimport type { ExecutionReplayMaterializationResult } from './executionReplayMaterializationTypes';")
    }

    if (-not $content.Contains("const [replayMaterialization, setReplayMaterialization]")) {
        $anchor = "const [replayPreparation, setReplayPreparation]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayMaterialization, setReplayMaterialization] = useState<ExecutionReplayMaterializationResult | null>(null);`r`n  const [replayApprovalNote, setReplayApprovalNote] = useState('');")
    }

    if (-not $content.Contains("async function materializeSelectedReplay()")) {
        $marker = "async function prepareSelectedReplayManifest()"
        $block = @'
  async function materializeSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await materializeExecutionReplay({
        sourceExecutionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        approvalNote: replayApprovalNote,
      });

      setReplayMaterialization(result);
      setReplayApprovalNote('');
      setStatusMessage(`Replay session materialized: ${result.replayExecutionSessionId}`);
      await loadSessions();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to materialize replay session.');
    }
  }

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Materialize replay")) {
        $anchor = '<button type="button" onClick={prepareSelectedReplayManifest}>Prepare replay manifest</button>'
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $replacement = '<button type="button" onClick={prepareSelectedReplayManifest}>Prepare replay manifest</button>' + "`r`n" +
            '            <label>Replay approval<input value={replayApprovalNote} onChange={(event) => setReplayApprovalNote(event.target.value)} placeholder="Required approval note" /></label>' + "`r`n" +
            '            <button type="button" onClick={materializeSelectedReplay} disabled={!replayApprovalNote.trim()}>Materialize replay</button>'
        $content = $content.Replace($anchor, $replacement)
    }

    if (-not $content.Contains("<h3>Replay materialized</h3>")) {
        $marker = "{replayPreparation ? ("
        $insert = @'
          {replayMaterialization ? (
            <div className="table-shell">
              <h3>Replay materialized</h3>
              <div className="metric-grid">
                <article><span>Replay session</span><strong>{replayMaterialization.replayExecutionSessionId}</strong></article>
                <article><span>Scope</span><strong>{replayMaterialization.scope}</strong></article>
                <article><span>Depth</span><strong>{replayMaterialization.replayDepth}</strong></article>
                <article><span>Work items</span><strong>{replayMaterialization.workItemCount}</strong></article>
              </div>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay materialization UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "database/sql/operational/008_add_execution_replay_lineage.sql"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/MaterializeExecutionReplayRequest.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayMaterializationResult.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayMaterializationService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayMaterializationEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayMaterializationTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayMaterializationApi.ts"
Copy-PayloadFile "docs/operations/P4.69-replay-execution-session-materialization.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce -Path $programPath -Line "builder.Services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();" -Anchor "builder.Services.AddScoped<IExecutionReplayPreparationService, SqlExecutionReplayPreparationService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce -Path $compositionPath -Line "        endpoints.MapExecutionReplayMaterializationEndpoints();" -Anchor "        endpoints.MapExecutionReplayPreparationEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplayMaterializationSupport -Path $workspacePath

Write-Step "Complete."
