[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.77] {0}" -f $Message) }

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

function Add-UiAdmissionSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay admission UI support"
        return
    }

    if (-not $content.Contains("evaluateExecutionReplayAdmission")) {
        $anchor = "import { materializeExecutionReplay } from './executionReplayMaterializationApi';"
        if (-not $content.Contains($anchor)) { throw ("UI import anchor not found: {0}" -f $anchor) }
        $content = $content.Replace(
            $anchor,
            "import { evaluateExecutionReplayAdmission } from './executionReplayAdmissionApi';`r`nimport type { ExecutionReplayAdmissionEvaluationResult } from './executionReplayAdmissionTypes';`r`n" + $anchor)
    }

    if (-not $content.Contains("const [replayAdmission, setReplayAdmission]")) {
        $anchor = "const [replayMaterialization, setReplayMaterialization]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI state anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayAdmission, setReplayAdmission] = useState<ExecutionReplayAdmissionEvaluationResult | null>(null);`r`n  const [replayAdmissionTake, setReplayAdmissionTake] = useState(25);")
    }

    if (-not $content.Contains("async function evaluateReplayAdmissionQueue()")) {
        $marker = "async function materializeSelectedReplay()"
        $block = @'
  async function evaluateReplayAdmissionQueue() {
    try {
      const result = await evaluateExecutionReplayAdmission({
        take: replayAdmissionTake,
      });

      setReplayAdmission(result);
      setStatusMessage(`Replay admission evaluated: ${result.decisions.length} decision(s).`);
      await loadSessions();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to evaluate replay admission.');
    }
  }

'@
        if (-not $content.Contains($marker)) { throw ("UI function marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Evaluate admission")) {
        $anchor = '<button type="button" onClick={materializeSelectedReplay}'
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI button anchor not found: {0}" -f $anchor) }
        $insert = @'
            <label>
              Admission take
              <input type="number" min="1" max="250" value={replayAdmissionTake} onChange={(event) => setReplayAdmissionTake(Number(event.target.value))} />
            </label>
            <button type="button" onClick={evaluateReplayAdmissionQueue}>Evaluate admission</button>
'@
        $content = $content.Insert($idx, $insert)
    }

    if (-not $content.Contains("<h3>Replay admission</h3>")) {
        $marker = "{replayMaterialization ? ("
        $insert = @'
          {replayAdmission ? (
            <div className="table-shell">
              <h3>Replay admission</h3>
              <div className="metric-grid">
                <article><span>Active replays</span><strong>{replayAdmission.activeReplayCount}</strong></article>
                <article><span>Max concurrent</span><strong>{replayAdmission.maxConcurrentReplays}</strong></article>
                <article><span>Allowed window</span><strong>{replayAdmission.withinAllowedWindow ? 'Yes' : 'No'}</strong></article>
                <article><span>Decisions</span><strong>{replayAdmission.decisions.length}</strong></article>
              </div>
              <table>
                <thead><tr><th>Created</th><th>Decision</th><th>Name</th><th>Reason</th><th>Session</th></tr></thead>
                <tbody>
                  {replayAdmission.decisions.length === 0 ? (
                    <tr><td colSpan={5}>No admission-pending replay sessions were evaluated.</td></tr>
                  ) : (
                    replayAdmission.decisions.map((decision) => (
                      <tr key={decision.executionSessionId}>
                        <td>{new Date(decision.createdUtc).toLocaleString()}</td>
                        <td>{decision.decision}</td>
                        <td>{decision.name}</td>
                        <td>{decision.reason}</td>
                        <td><code>{decision.executionSessionId}</code></td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI panel marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay admission UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionApi.ts"
Copy-PayloadFile "docs/ui/P4.77-replay-admission-operator-ui.md"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiAdmissionSupport -Path $workspacePath

Write-Step "Complete."
