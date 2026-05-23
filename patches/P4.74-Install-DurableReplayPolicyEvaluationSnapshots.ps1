[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.74] {0}" -f $Message) }

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

function Add-UiPolicyHistorySupport {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $Apply) { Write-Step "WOULD add replay policy history UI support"; return }

    if (-not $content.Contains("fetchExecutionReplayPolicyHistory")) {
        $content = $content.Replace(
            "import { evaluateExecutionReplayPolicy } from './executionReplayPolicyApi';",
            "import { evaluateExecutionReplayPolicy, fetchExecutionReplayPolicyHistory } from './executionReplayPolicyApi';")
    }

    if (-not $content.Contains("ExecutionReplayPolicyEvaluationRecord")) {
        $content = $content.Replace(
            "import type { ExecutionReplayPolicyEvaluationResult } from './executionReplayPolicyTypes';",
            "import type { ExecutionReplayPolicyEvaluationRecord, ExecutionReplayPolicyEvaluationResult } from './executionReplayPolicyTypes';")
    }

    if (-not $content.Contains("const [replayPolicyHistory, setReplayPolicyHistory]")) {
        $anchor = "const [replayPolicy, setReplayPolicy]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayPolicyHistory, setReplayPolicyHistory] = useState<ExecutionReplayPolicyEvaluationRecord[]>([]);")
    }

    if (-not $content.Contains("fetchExecutionReplayPolicyHistory(session.executionSessionId")) {
        $anchor = "setReplayLineage(lineageResponse);"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, $anchor + "`r`n      const policyHistoryResponse = await fetchExecutionReplayPolicyHistory(session.executionSessionId, 25);`r`n      setReplayPolicyHistory(policyHistoryResponse.evaluations);")
    }

    if (-not $content.Contains("<h3>Replay policy history</h3>")) {
        $marker = "{replayPolicy ? ("
        $insert = @'
          <div className="table-shell">
            <h3>Replay policy history</h3>
            <table>
              <thead><tr><th>Created</th><th>Decision</th><th>Scope</th><th>Score</th></tr></thead>
              <tbody>
                {replayPolicyHistory.length === 0 ? (
                  <tr><td colSpan={4}>No replay policy evaluations have been recorded for this session.</td></tr>
                ) : (
                  replayPolicyHistory.map((evaluation) => (
                    <tr key={evaluation.replayPolicyEvaluationId}>
                      <td>{new Date(evaluation.createdUtc).toLocaleString()}</td>
                      <td>{evaluation.decision}</td>
                      <td>{evaluation.scope}</td>
                      <td>{evaluation.policyScore}</td>
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
    Write-Step "Added replay policy history UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "database/sql/operational/010_create_execution_replay_policy_evaluations.sql"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayPolicyModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayPolicyService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPolicyService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayPolicyEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPolicyTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPolicyApi.ts"
Copy-PayloadFile "docs/operations/P4.74-durable-replay-policy-evaluation-snapshots.md"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiPolicyHistorySupport -Path $workspacePath

Write-Step "Complete."
