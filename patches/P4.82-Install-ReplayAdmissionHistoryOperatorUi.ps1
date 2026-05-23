[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.82] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-UiAdmissionHistorySupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay admission history UI support"
        return
    }

    if (-not $content.Contains("fetchExecutionReplayAdmissionHistory")) {
        $content = $content.Replace(
            "import { evaluateExecutionReplayAdmission } from './executionReplayAdmissionApi';",
            "import { evaluateExecutionReplayAdmission, fetchExecutionReplayAdmissionHistory } from './executionReplayAdmissionApi';")
    }

    if (-not $content.Contains("ExecutionReplayAdmissionDecisionRecord")) {
        $content = $content.Replace(
            "import type { ExecutionReplayAdmissionEvaluationResult } from './executionReplayAdmissionTypes';",
            "import type { ExecutionReplayAdmissionDecisionRecord, ExecutionReplayAdmissionEvaluationResult } from './executionReplayAdmissionTypes';")
    }

    if (-not $content.Contains("const [replayAdmissionHistory, setReplayAdmissionHistory]")) {
        $anchor = "const [replayAdmission, setReplayAdmission]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) {
            throw ("UI state anchor not found: {0}" -f $anchor)
        }

        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayAdmissionHistory, setReplayAdmissionHistory] = useState<ExecutionReplayAdmissionDecisionRecord[]>([]);")
    }

    if (-not $content.Contains("fetchExecutionReplayAdmissionHistory(session.executionSessionId")) {
        $anchor = "setReplayLineage(lineageResponse);"
        if (-not $content.Contains($anchor)) {
            throw ("UI load anchor not found: {0}" -f $anchor)
        }

        $content = $content.Replace(
            $anchor,
            $anchor + "`r`n      const admissionHistoryResponse = await fetchExecutionReplayAdmissionHistory(session.executionSessionId, 25);`r`n      setReplayAdmissionHistory(admissionHistoryResponse.decisions);")
    }

    if (-not $content.Contains("<h3>Replay admission history</h3>")) {
        $marker = "{replayAdmission ? ("
        $insert = @'
          <div className="table-shell">
            <h3>Replay admission history</h3>
            <table>
              <thead><tr><th>Created</th><th>Decision</th><th>Reason</th><th>Active</th><th>Limit</th><th>Window</th></tr></thead>
              <tbody>
                {replayAdmissionHistory.length === 0 ? (
                  <tr><td colSpan={6}>No replay admission decisions have been recorded for this session.</td></tr>
                ) : (
                  replayAdmissionHistory.map((decision) => (
                    <tr key={decision.replayAdmissionDecisionId}>
                      <td>{new Date(decision.createdUtc).toLocaleString()}</td>
                      <td>{decision.decision}</td>
                      <td>{decision.reason}</td>
                      <td>{decision.activeReplayCount}</td>
                      <td>{decision.maxConcurrentReplays}</td>
                      <td>{decision.withinAllowedWindow ? 'Yes' : 'No'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

'@
        if (-not $content.Contains($marker)) {
            throw ("UI panel marker not found: {0}" -f $marker)
        }

        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay admission history UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "docs/ui/P4.82-replay-admission-history-operator-ui.md"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiAdmissionHistorySupport -Path $workspacePath

Write-Step "Complete."
