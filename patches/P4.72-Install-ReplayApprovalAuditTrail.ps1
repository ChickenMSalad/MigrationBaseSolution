[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.72] {0}" -f $Message) }

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

function Add-UiApprovalHistorySupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) { Write-Step "WOULD add replay approval audit UI support"; return }

    if (-not $content.Contains("fetchExecutionReplayApprovalHistory")) {
        $anchor = "import { approveExecutionReplay } from './executionReplayApprovalApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, "import { approveExecutionReplay, fetchExecutionReplayApprovalHistory } from './executionReplayApprovalApi';")
    }

    if (-not $content.Contains("ExecutionReplayApprovalRecord")) {
        $anchor = "import type { ExecutionReplayApprovalResult } from './executionReplayApprovalTypes';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, "import type { ExecutionReplayApprovalRecord, ExecutionReplayApprovalResult } from './executionReplayApprovalTypes';")
    }

    if (-not $content.Contains("const [replayApprovalHistory, setReplayApprovalHistory]")) {
        $anchor = "const [replayApproval, setReplayApproval]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayApprovalHistory, setReplayApprovalHistory] = useState<ExecutionReplayApprovalRecord[]>([]);")
    }

    if (-not $content.Contains("fetchExecutionReplayApprovalHistory(session.executionSessionId")) {
        $anchor = "setReplayLineage(lineageResponse);"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, $anchor + "`r`n      const approvalHistoryResponse = await fetchExecutionReplayApprovalHistory(session.executionSessionId, 25);`r`n      setReplayApprovalHistory(approvalHistoryResponse.approvals);")
    }

    if (-not $content.Contains("<h3>Replay approval audit trail</h3>")) {
        $marker = "{replayApproval ? ("
        $insert = @'
          <div className="table-shell">
            <h3>Replay approval audit trail</h3>
            <table>
              <thead><tr><th>Created</th><th>Status</th><th>Scope</th><th>Approved by</th><th>Expires</th><th>Replay session</th></tr></thead>
              <tbody>
                {replayApprovalHistory.length === 0 ? (
                  <tr><td colSpan={6}>No replay approvals have been recorded for this session.</td></tr>
                ) : (
                  replayApprovalHistory.map((approval) => (
                    <tr key={approval.replayApprovalId}>
                      <td>{new Date(approval.createdUtc).toLocaleString()}</td>
                      <td>{approval.status}</td>
                      <td>{approval.scope}</td>
                      <td>{approval.approvedBy}</td>
                      <td>{new Date(approval.expiresUtc).toLocaleString()}</td>
                      <td>{approval.replayExecutionSessionId ? <code>{approval.replayExecutionSessionId}</code> : '—'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay approval audit UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayApprovalService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayApprovalService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayApprovalEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayApprovalTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayApprovalApi.ts"
Copy-PayloadFile "docs/operations/P4.72-replay-approval-audit-trail.md"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiApprovalHistorySupport -Path $workspacePath

Write-Step "Complete."
