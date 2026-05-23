[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.71] {0}" -f $Message) }

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

function Add-UiReplayApprovalSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) { Write-Step "WOULD add replay approval UI support"; return }

    if (-not $content.Contains("approveExecutionReplay")) {
        $anchor = "import { materializeExecutionReplay } from './executionReplayMaterializationApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, "import { approveExecutionReplay } from './executionReplayApprovalApi';`r`nimport type { ExecutionReplayApprovalResult } from './executionReplayApprovalTypes';`r`n" + $anchor)
    }

    if (-not $content.Contains("const [replayApproval, setReplayApproval]")) {
        $anchor = "const [replayMaterialization, setReplayMaterialization]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayApproval, setReplayApproval] = useState<ExecutionReplayApprovalResult | null>(null);`r`n  const [replayApprovedBy, setReplayApprovedBy] = useState('operator');`r`n  const [replayApprovalMinutes, setReplayApprovalMinutes] = useState(60);")
    }

    if (-not $content.Contains("async function approveSelectedReplay()")) {
        $marker = "async function materializeSelectedReplay()"
        $block = @'
  async function approveSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await approveExecutionReplay({
        sourceExecutionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        approvedBy: replayApprovedBy,
        approvalNote: replayApprovalNote,
        expiresInMinutes: replayApprovalMinutes,
      });

      setReplayApproval(result);
      setStatusMessage(`Replay approved until ${new Date(result.approval.expiresUtc).toLocaleString()}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve replay.');
    }
  }

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Approve replay")) {
        $anchor = '<button type="button" onClick={materializeSelectedReplay} disabled={!replayApprovalNote.trim()}>Materialize replay</button>'
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $replacement = '<label>Approved by<input value={replayApprovedBy} onChange={(event) => setReplayApprovedBy(event.target.value)} placeholder="Operator" /></label>' + "`r`n" +
            '            <label>Approval minutes<input type="number" min="5" max="1440" value={replayApprovalMinutes} onChange={(event) => setReplayApprovalMinutes(Number(event.target.value))} /></label>' + "`r`n" +
            '            <button type="button" onClick={approveSelectedReplay} disabled={!replayApprovalNote.trim() || !replayApprovedBy.trim()}>Approve replay</button>' + "`r`n" +
            '            ' + $anchor
        $content = $content.Replace($anchor, $replacement)
    }

    if (-not $content.Contains("<h3>Replay approval</h3>")) {
        $marker = "{replayMaterialization ? ("
        $insert = @'
          {replayApproval ? (
            <div className="table-shell">
              <h3>Replay approval</h3>
              <div className="metric-grid">
                <article><span>Approval</span><strong>{replayApproval.approval.replayApprovalId}</strong></article>
                <article><span>Status</span><strong>{replayApproval.approval.status}</strong></article>
                <article><span>Approved by</span><strong>{replayApproval.approval.approvedBy}</strong></article>
                <article><span>Expires</span><strong>{new Date(replayApproval.approval.expiresUtc).toLocaleString()}</strong></article>
              </div>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay approval UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "database/sql/operational/009_create_execution_replay_approvals.sql"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayApprovalModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayApprovalService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayApprovalService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayApprovalEndpointExtensions.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayApprovalTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayApprovalApi.ts"
Copy-PayloadFile "docs/operations/P4.71-replay-governance-approval-controls.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce -Path $programPath -Line "builder.Services.AddScoped<IExecutionReplayApprovalService, SqlExecutionReplayApprovalService>();" -Anchor "builder.Services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce -Path $compositionPath -Line "        endpoints.MapExecutionReplayApprovalEndpoints();" -Anchor "        endpoints.MapExecutionReplayMaterializationEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplayApprovalSupport -Path $workspacePath

Write-Step "Complete."
